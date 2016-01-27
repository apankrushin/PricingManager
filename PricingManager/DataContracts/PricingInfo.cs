namespace PricingManager.DataContracts
{
    public class PricingInfo
    {
        public string Message { get; set; }
        public StatusEnum Result { get; set; }
    }

    public enum StatusEnum
    {
        Changed,
        Confirmed,
    }
}
