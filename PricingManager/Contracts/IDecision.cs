using System.Threading.Tasks;

namespace PricingManager.Contracts
{
    public interface IDecision
    {
        Task ProcessDecisionAsync(IOrder order);
    }
}
