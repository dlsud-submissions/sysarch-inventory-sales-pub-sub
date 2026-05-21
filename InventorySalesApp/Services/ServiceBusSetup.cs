using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Configuration;

namespace InventorySalesApp.Services
{
    public class ServiceBusSetup
    {
        private readonly ServiceBusAdministrationClient _adminClient;
        private readonly string _topicName;
        private readonly string _accountingSubscription;
        private readonly string _supplierSubscription;
        private readonly string _inventorySubscription;

        // Production constructor
        public ServiceBusSetup(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("ServiceBus");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ServiceBus connection string is not configured in ConnectionStrings:ServiceBus");
            }

            _topicName = configuration["ServiceBus:TopicName"] ?? "orders";
            _accountingSubscription = configuration["ServiceBus:AccountingSubscription"] ?? "accounting-subscription";
            _supplierSubscription = configuration["ServiceBus:SupplierSubscription"] ?? "supplier-subscription";
            _inventorySubscription = configuration["ServiceBus:InventorySubscription"] ?? "inventory-subscription";

            _adminClient = new ServiceBusAdministrationClient(connectionString);
        }

        // Constructor for unit tests to inject a mock admin client
        public ServiceBusSetup(ServiceBusAdministrationClient adminClient, string topicName,
            string accountingSubscription = "accounting-subscription",
            string supplierSubscription = "supplier-subscription",
            string inventorySubscription = "inventory-subscription")
        {
            _adminClient = adminClient ?? throw new ArgumentNullException(nameof(adminClient));
            _topicName = topicName ?? throw new ArgumentNullException(nameof(topicName));
            _accountingSubscription = accountingSubscription;
            _supplierSubscription = supplierSubscription;
            _inventorySubscription = inventorySubscription;
        }

        public async Task ConfigureFiltersAsync()
        {
            // Inventory should receive every order. Recreate this rule if the portal or a prior test run removed it.
            try
            {
                var inventoryRule = new CreateRuleOptions("AllOrders", new TrueRuleFilter());
                await _adminClient.CreateRuleAsync(_topicName, _inventorySubscription, inventoryRule).ConfigureAwait(false);
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
            {
                // Ignore if already exists.
            }
            catch (Azure.RequestFailedException)
            {
                // Ignore if already exists.
            }

            // For accounting subscription: remove $Default and add PrepaidOrdersOnly
            try
            {
                await _adminClient.DeleteRuleAsync(_topicName, _accountingSubscription, "$Default").ConfigureAwait(false);
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
            {
                // Ignore if rule does not exist.
            }
            catch (Azure.RequestFailedException)
            {
                // Ignore if rule does not exist or other request failed
            }

            try
            {
                var prepaidRule = new CreateRuleOptions("PrepaidOrdersOnly", new SqlRuleFilter("OrderType = 'Prepaid'"));
                await _adminClient.CreateRuleAsync(_topicName, _accountingSubscription, prepaidRule).ConfigureAwait(false);
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
            {
                // Ignore if already exists.
            }
            catch (Azure.RequestFailedException)
            {
                // Ignore if already exists
            }

            // For supplier subscription: remove $Default and add LowStockOnly
            try
            {
                await _adminClient.DeleteRuleAsync(_topicName, _supplierSubscription, "$Default").ConfigureAwait(false);
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
            {
                // Ignore if rule does not exist.
            }
            catch (Azure.RequestFailedException)
            {
                // Ignore
            }

            try
            {
                var lowStockRule = new CreateRuleOptions("LowStockOnly", new SqlRuleFilter("Status = 'LowStock'"));
                await _adminClient.CreateRuleAsync(_topicName, _supplierSubscription, lowStockRule).ConfigureAwait(false);
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
            {
                // Ignore if already exists.
            }
            catch (Azure.RequestFailedException)
            {
                // Ignore
            }
        }
    }
}
