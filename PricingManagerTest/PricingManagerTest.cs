using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using PricingManager.Contracts;
using PricingManager.DataContracts;

namespace TempTest
{
    [TestFixture]
    public class PricingManagerTest
    {
        private IPricingManager _pricingManager;
        private IAviaService _aviaService;
        private IDpService _dpService;
        private ICriteriaFactory _criteriaFactory;
        private IDecisionFactory _decisionFactory;


        [SetUp]
        public void TestSetup()
        {
            _aviaService = Substitute.For<IAviaService>();
            _dpService = Substitute.For<IDpService>();
            _criteriaFactory = Substitute.For<ICriteriaFactory>();
            _decisionFactory = Substitute.For<IDecisionFactory>();

            _pricingManager = new PricingManager.PricingManager(_aviaService, _dpService, _criteriaFactory, _decisionFactory);
        }

        [Test]
        public void Repricing_InvalidTickets_ExceptionThrown()
        {
            var order = new Order();
            var callDelegate = new TestDelegate(() => _pricingManager.RepricingAsync(order).Wait());

            Assert.Throws<AggregateException>(callDelegate);
            Assert.Throws<AggregateException>(callDelegate).InnerExceptions.Any(exception => exception is TicketNotValidException);
        }

        [Test]
        public void Repricing_ValidPipeline_UpdatePriceCalled()
        {
            _aviaService.IsTicketValidAsync(Arg.Any<string>()).Returns(info => Task.FromResult(true));
            _dpService.IsTicketValidAsync(Arg.Any<string>()).Returns(info => Task.FromResult(true));
            _aviaService.UpdateTicketPriceAsync(Arg.Any<string>()).Returns(info => Task.FromResult(new AviaPricesInfo() as IPriceObject));
            _dpService.UpdateBookingPriceAsync(Arg.Any<string>()).Returns(info => Task.FromResult(new HotelPricesInfo() as IPriceObject));

            _pricingManager.RepricingAsync(new Order()).Wait();

            _aviaService.ReceivedWithAnyArgs(1).UpdateTicketPriceAsync(Arg.Any<string>());
            _dpService.ReceivedWithAnyArgs(1).UpdateBookingPriceAsync(Arg.Any<string>());
        }

        [Test]
        public void Repricing_HotelBooked_UpdatePriceCalledForAviaOnly()
        {
            _aviaService.IsTicketValidAsync(Arg.Any<string>()).Returns(info => Task.FromResult(true));
            _dpService.IsTicketValidAsync(Arg.Any<string>()).Returns(info => Task.FromResult(true));
            _aviaService.UpdateTicketPriceAsync(Arg.Any<string>()).Returns(info => Task.FromResult(new AviaPricesInfo() as IPriceObject));
            _dpService.UpdateBookingPriceAsync(Arg.Any<string>()).Returns(info => Task.FromResult(new HotelPricesInfo() as IPriceObject));

            _pricingManager.RepricingAsync(new Order() {IsHotelBooked = true}).Wait();

            _aviaService.ReceivedWithAnyArgs(1).UpdateTicketPriceAsync(Arg.Any<string>());
            _dpService.DidNotReceiveWithAnyArgs().UpdateBookingPriceAsync(Arg.Any<string>());
        }

        [Test]
        public void Repricing_DeltaHasNoChanges_MessageWithNoChanges()
        {
            var testOrder = new Order() {Price = 1000};
            _aviaService.IsTicketValidAsync(Arg.Any<string>()).Returns(info => Task.FromResult(true));
            _dpService.IsTicketValidAsync(Arg.Any<string>()).Returns(info => Task.FromResult(true));
            _aviaService.UpdateTicketPriceAsync(Arg.Any<string>()).Returns(info => new AviaPricesInfo {Price = testOrder.Price * 0.4});
            _dpService.UpdateBookingPriceAsync(Arg.Any<string>()).Returns(info => new HotelPricesInfo { Price = testOrder.Price * 0.6 });

            var repricingTask = _pricingManager.RepricingAsync(testOrder);
            repricingTask.Wait();
            var repricingInfo = repricingTask.Result;

            Assert.That(repricingInfo.Result, Is.EqualTo(StatusEnum.Confirmed));
        }

        [Test]
        public void Repricing_DeltaHasChanged_CriteriaChecked()
        {
            var testOrder = new Order() { Price = 1000 };
            var testCriteria = new FakeCriteria();
            
            _aviaService.IsTicketValidAsync(Arg.Any<string>()).Returns(info => Task.FromResult(true));
            _dpService.IsTicketValidAsync(Arg.Any<string>()).Returns(info => Task.FromResult(true));
            _aviaService.UpdateTicketPriceAsync(Arg.Any<string>()).Returns(info => new AviaPricesInfo { Price = testOrder.Price * 0.1 });
            _dpService.UpdateBookingPriceAsync(Arg.Any<string>()).Returns(info => new HotelPricesInfo { Price = testOrder.Price * 0.6 });
            _criteriaFactory.GetCriterias(Arg.Any<IOrder>()).Returns(info => new List<ICriteria> {testCriteria});
            _decisionFactory.GetDecisions(Arg.Any<ICriteria>()).Returns(info => new List<IDecision>());

            var repricingTask = _pricingManager.RepricingAsync(testOrder);
            repricingTask.Wait();
            var repricingInfo = repricingTask.Result;

            Assert.That(repricingInfo.Result, Is.EqualTo(StatusEnum.Changed));
            Assert.AreEqual(testCriteria.CheckCount, 1);
        }

        [Test]
        public void Repricing_DeltaHasChanged_DecisionMade()
        {
            var testOrder = new Order() { Price = 1000 };
            var testCriteria = new FakeCriteria();
            var testDecision = Substitute.For<IDecision>();
            
            _aviaService.IsTicketValidAsync(Arg.Any<string>()).Returns(info => Task.FromResult(true));
            _dpService.IsTicketValidAsync(Arg.Any<string>()).Returns(info => Task.FromResult(true));
            _aviaService.UpdateTicketPriceAsync(Arg.Any<string>()).Returns(info => new AviaPricesInfo { Price = testOrder.Price * 0.1 });
            _dpService.UpdateBookingPriceAsync(Arg.Any<string>()).Returns(info => new HotelPricesInfo { Price = testOrder.Price * 0.6 });
            _criteriaFactory.GetCriterias(Arg.Any<IOrder>()).Returns(info => new List<ICriteria> { testCriteria });
            _decisionFactory.GetDecisions(Arg.Any<FakeCriteria>()).Returns(info => new List<IDecision> { testDecision });

            var repricingTask = _pricingManager.RepricingAsync(testOrder);
            repricingTask.Wait();
            var repricingInfo = repricingTask.Result;

            Assert.That(repricingInfo.Result, Is.EqualTo(StatusEnum.Changed));
            testDecision.ReceivedWithAnyArgs(1).ProcessDecisionAsync(Arg.Any<IOrder>());
        }
    }

    internal class FakeCriteria : ICriteria
    {
        public int CheckCount { get; set; }

        public Task<bool> CheckCriteriaAsync(PriceInfoObject oldPriceInfo, PriceInfoObject newPriceInfo)
        {
            CheckCount++;
            return Task.FromResult(true);
        }

        public int Priority { get { return 0; } }
    }
}
