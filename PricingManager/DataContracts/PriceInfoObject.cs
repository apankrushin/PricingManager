using PricingManager.Contracts;

namespace PricingManager.DataContracts
{
    public class PriceInfoObject
    {
        public AviaPricesInfo AviaPricesInfo { get; set; }
        public HotelPricesInfo HotelPricesInfo { get; set; }
    }

    public class AviaPricesInfo : IPriceObject
    {
        public double Price { get; set; }
    }

    public class HotelPricesInfo : IPriceObject
    {
        public double Price { get; set; }
    }
}