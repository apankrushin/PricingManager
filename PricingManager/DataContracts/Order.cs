using PricingManager.Contracts;

namespace PricingManager.DataContracts
{
    public class Order : IOrder
    {
        public string AviaBookingNumber { get; set; }
        public string HotelBookingNumber { get; set; }
        public bool IsHotelBooked { get; set; }
        public double Price { get; set; }
    }
}
