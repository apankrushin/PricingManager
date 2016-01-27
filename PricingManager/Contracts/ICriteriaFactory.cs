using System.Collections;
using System.Collections.Generic;

namespace PricingManager.Contracts
{
    public interface ICriteriaFactory
    {
        IList<ICriteria> GetCriterias(IOrder order);
    }
}
