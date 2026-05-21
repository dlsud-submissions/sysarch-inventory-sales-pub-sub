using InventorySalesApp.Models;

namespace InventorySalesApp.Services
{
    public interface ISubscriptionStatsService
    {
        Task<List<SubscriptionStatsViewModel>> GetAllSubscriptionStatsAsync();
    }
}
