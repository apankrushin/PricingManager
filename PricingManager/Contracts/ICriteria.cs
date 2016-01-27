using System.Collections.Generic;
using System.Threading.Tasks;
using PricingManager.DataContracts;

namespace PricingManager.Contracts
{
    public interface ICriteria
    {
        Task<bool> CheckCriteriaAsync(PriceInfoObject oldPriceInfo, PriceInfoObject newPriceInfo);
        int Priority { get; }
        //IEnumerable<IPricingDecision> Decisions { get; }
    }
}
