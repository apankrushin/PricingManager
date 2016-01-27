namespace PricingManager.Contracts
{
    public interface IOrder
    {
        string AviaBookingNumber { get; set; }
        string HotelBookingNumber { get; set; }
        bool IsHotelBooked { get; set; }
        double Price { get; set; }
    }
}