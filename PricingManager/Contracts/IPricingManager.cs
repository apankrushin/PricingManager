using System.Threading.Tasks;
using PricingManager.DataContracts;

namespace PricingManager.Contracts
{
    public interface IPricingManager
    {
        Task<PricingInfo> RepricingAsync(IOrder order);
    }
}
