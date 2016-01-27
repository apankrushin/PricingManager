using System.Threading.Tasks;

namespace PricingManager.Contracts
{
    public interface IService
    {
        Task<bool> IsTicketValidAsync(string bookingNumber);
    }

    public interface IAviaService : IService
    {
        Task<IPriceObject> UpdateTicketPriceAsync(string aviaBookingNumber);
    }

    public interface IDpService : IService
    {
        Task<IPriceObject> UpdateBookingPriceAsync(string dpBookingNumber);
    }
}