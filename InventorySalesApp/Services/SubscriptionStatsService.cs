using Azure.Messaging.ServiceBus.Administration;
using InventorySalesApp.Models;

namespace InventorySalesApp.Services
{
    public class SubscriptionStatsService : ISubscriptionStatsService
    {
        private readonly ServiceBusAdministrationClient _adminClient;
        private readonly string _topicName;
        private readonly string[] _subscriptionNames;

        public SubscriptionStatsService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("ServiceBus");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ServiceBus connection string is not configured in ConnectionStrings:ServiceBus");
            }

            _topicName = configuration["ServiceBus:TopicName"] ?? "orders";
            _subscriptionNames =
            [
                configuration["ServiceBus:InventorySubscription"] ?? "inventory-subscription",
                configuration["ServiceBus:AccountingSubscription"] ?? "accounting-subscription",
                configuration["ServiceBus:SupplierSubscription"] ?? "supplier-subscription"
            ];
            _adminClient = new ServiceBusAdministrationClient(connectionString);
        }

        public SubscriptionStatsService(
            ServiceBusAdministrationClient adminClient,
            string topicName,
            string inventorySubscription = "inventory-subscription",
            string accountingSubscription = "accounting-subscription",
            string supplierSubscription = "supplier-subscription")
        {
            _adminClient = adminClient ?? throw new ArgumentNullException(nameof(adminClient));
            _topicName = topicName ?? throw new ArgumentNullException(nameof(topicName));
            _subscriptionNames = [inventorySubscription, accountingSubscription, supplierSubscription];
        }

        public async Task<List<SubscriptionStatsViewModel>> GetAllSubscriptionStatsAsync()
        {
            var stats = new List<SubscriptionStatsViewModel>();

            foreach (var subscriptionName in _subscriptionNames)
            {
                try
                {
                    var props = await _adminClient
                        .GetSubscriptionRuntimePropertiesAsync(_topicName, subscriptionName)
                        .ConfigureAwait(false);

                    stats.Add(new SubscriptionStatsViewModel
                    {
                        SubscriptionName = subscriptionName,
                        ActiveMessageCount = props.Value.ActiveMessageCount,
                        DeadLetterMessageCount = props.Value.DeadLetterMessageCount,
                        TransferMessageCount = props.Value.TransferMessageCount,
                        TotalMessageCount = props.Value.TotalMessageCount,
                        Status = "Active",
                        LastRefreshed = DateTime.UtcNow
                    });
                }
                catch
                {
                    // Keep the dashboard resilient: one failed subscription should not block the rest.
                }
            }

            return stats;
        }
    }
}
