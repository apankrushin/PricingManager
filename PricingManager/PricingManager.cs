using System;
using System.Linq;
using System.Threading.Tasks;
using PricingManager.Contracts;
using PricingManager.DataContracts;

namespace PricingManager
{
    public class PricingManager : IPricingManager
    {
        private readonly IAviaService _aviaService;
        private readonly IDpService _dpService;
        private ICriteriaFactory _criteriaFactory;
        private readonly IDecisionFactory _decisionFactory;

        public PricingManager(IAviaService aviaService, IDpService dpService, ICriteriaFactory criteriaFactory, IDecisionFactory decisionFactory)
        {
            if(aviaService==null)
                throw new ArgumentNullException("aviaService");
            if (dpService == null)
                throw new ArgumentNullException("dpService");
            if (criteriaFactory == null)
                throw new ArgumentNullException("criteriaFactory");

            _aviaService = aviaService;
            _dpService = dpService;
            _criteriaFactory = criteriaFactory;
            _decisionFactory = decisionFactory;
        }

        public async Task<PricingInfo> RepricingAsync(IOrder order)
        {
            await TicketsValidation(order.AviaBookingNumber, order.HotelBookingNumber);

            var pricesInfoObject = await UpdatePricesAsync(order);

            var delta = CalculateDelta(order, pricesInfoObject);
            if (Math.Abs(delta) < double.Epsilon)
                return new PricingInfo {Result = StatusEnum.Confirmed, Message = "Цена не изменилась"};

            return await ProcessPriceChangesAsync(order, pricesInfoObject);
        }

        private async Task TicketsValidation(string aviaBookingNumber, string dpBookingNumber)
        {
            var validationTasks = new[]
            {
                _aviaService.IsTicketValidAsync(aviaBookingNumber),
                _dpService.IsTicketValidAsync(dpBookingNumber)
            };
            await Task.WhenAll(validationTasks);

            if (!validationTasks[0].Result)
                throw new TicketNotValidException("Билеты на самолет более не доступны");
            if (!validationTasks[1].Result)
                throw new TicketNotValidException("Номер отеля более не доступен");
        }

        private async Task<PriceInfoObject> UpdatePricesAsync(IOrder order)
        {
            var validationTasks = order.IsHotelBooked  == false ? new[]
            {
                _aviaService.UpdateTicketPriceAsync(order.AviaBookingNumber),
                _dpService.UpdateBookingPriceAsync(order.HotelBookingNumber)
            } : new [] { _aviaService.UpdateTicketPriceAsync(order.AviaBookingNumber), Task.FromResult<IPriceObject>(null)};
            await Task.WhenAll(validationTasks);

            return new PriceInfoObject{AviaPricesInfo = validationTasks[0].Result as AviaPricesInfo, HotelPricesInfo = validationTasks[1].Result as HotelPricesInfo};
        }

        private double CalculateDelta(IOrder order, PriceInfoObject pricesInfoObject)
        {
            return order.IsHotelBooked == false ?
                order.Price - pricesInfoObject.AviaPricesInfo.Price - pricesInfoObject.HotelPricesInfo.Price
                : order.Price - pricesInfoObject.AviaPricesInfo.Price;
        }

        private async Task<PricingInfo> ProcessPriceChangesAsync(IOrder order, PriceInfoObject pricesInfoObject)
        {
            var criterias = _criteriaFactory.GetCriterias(order);
            if (!criterias.Any())
                return await Task.FromResult(new PricingInfo { Result = StatusEnum.Changed, Message = "Цена изменилась" });

            var oldPricesInfoObject = GetPricesInfo(order);

            var criteriaResultsTask = criterias.OrderBy(criteria => criteria.Priority).Select(criteria => new { criteria = criteria, Task = criteria.CheckCriteriaAsync(oldPricesInfoObject, pricesInfoObject) } ).ToList();
            await Task.WhenAll(criteriaResultsTask.Select(arg => arg.Task));

            var mostImportantCriteria = criteriaResultsTask.FirstOrDefault(arg => arg.Task.Exception==null && arg.Task.Result);
            if (mostImportantCriteria == null)
                throw new AggregateException(criteriaResultsTask.Select(arg => arg.Task.Exception));

            var makeDecisionTasks = _decisionFactory.GetDecisions(mostImportantCriteria.criteria).Select(decision => decision.ProcessDecisionAsync(order));
            await Task.WhenAll(makeDecisionTasks);

            return await Task.FromResult(new PricingInfo { Result = StatusEnum.Changed, Message = "Цена изменилась" });
        }

        private PriceInfoObject GetPricesInfo(IOrder order)
        {
            return new PriceInfoObject();
        }
    }
}
