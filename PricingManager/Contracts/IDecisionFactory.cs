using System.Collections.Generic;

namespace PricingManager.Contracts
{
    public interface IDecisionFactory
    {
        IList<IDecision> GetDecisions(ICriteria criteria);
    }
}
