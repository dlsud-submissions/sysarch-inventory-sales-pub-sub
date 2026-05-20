using System.Text.Json;
using Azure.Messaging.ServiceBus;
using InventorySalesApp.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace InventorySalesApp.Tests.Integration
{
    public class ServiceBusIntegrationTests : IAsyncLifetime
    {
        private const string TopicName = "orders";
        private const string InventorySubscription = "inventory-subscription";
        private const string AccountingSubscription = "accounting-subscription";
        private const string SupplierSubscription = "supplier-subscription";
        private const int DeliveryWaitMilliseconds = 2500;

        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;

        public ServiceBusIntegrationTests()
        {
            var connectionString = GetServiceBusConnectionString();
            _client = new ServiceBusClient(connectionString);
            _sender = _client.CreateSender(TopicName);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Publish_PrepaidOrder_ShouldReach_InventorySubscription()
        {
            var order = CreateOrder(orderType: "Prepaid", status: "Pending");

            await PublishOrderAsync(order);

            Assert.True(
                await SubscriptionHasOrderAsync(InventorySubscription, order.OrderId),
                "Prepaid orders should reach the inventory subscription.");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Publish_PrepaidOrder_ShouldReach_AccountingSubscription()
        {
            var order = CreateOrder(orderType: "Prepaid", status: "Pending");

            await PublishOrderAsync(order);

            Assert.True(
                await SubscriptionHasOrderAsync(AccountingSubscription, order.OrderId),
                "Prepaid orders should reach the accounting subscription.");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Publish_PrepaidOrder_ShouldNotReach_SupplierSubscription()
        {
            var order = CreateOrder(orderType: "Prepaid", status: "Pending");

            await PublishOrderAsync(order);

            Assert.False(
                await SubscriptionHasOrderAsync(SupplierSubscription, order.OrderId),
                "Prepaid non-LowStock orders should not reach the supplier subscription.");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Publish_LowStockOrder_ShouldReach_InventorySubscription()
        {
            var order = CreateOrder(orderType: "Prepaid", status: "LowStock");

            await PublishOrderAsync(order);

            Assert.True(
                await SubscriptionHasOrderAsync(InventorySubscription, order.OrderId),
                "LowStock orders should reach the inventory subscription.");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Publish_LowStockOrder_ShouldReach_SupplierSubscription()
        {
            var order = CreateOrder(orderType: "Prepaid", status: "LowStock");

            await PublishOrderAsync(order);

            Assert.True(
                await SubscriptionHasOrderAsync(SupplierSubscription, order.OrderId),
                "LowStock orders should reach the supplier subscription.");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Publish_COD_Order_ShouldReach_InventorySubscription_Only()
        {
            var order = CreateOrder(orderType: "COD", status: "Fulfilled");

            await PublishOrderAsync(order);

            Assert.True(
                await SubscriptionHasOrderAsync(InventorySubscription, order.OrderId),
                "COD orders should reach the inventory subscription.");

            Assert.False(
                await SubscriptionHasOrderAsync(AccountingSubscription, order.OrderId),
                "COD orders should not reach the accounting subscription.");

            Assert.False(
                await SubscriptionHasOrderAsync(SupplierSubscription, order.OrderId),
                "COD non-LowStock orders should not reach the supplier subscription.");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Publish_COD_Order_ShouldNotReach_AccountingSubscription()
        {
            var order = CreateOrder(orderType: "COD", status: "Pending");

            await PublishOrderAsync(order);

            Assert.False(
                await SubscriptionHasOrderAsync(AccountingSubscription, order.OrderId),
                "COD orders should not reach the accounting subscription.");
        }

        public async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _sender.DisposeAsync();
            await _client.DisposeAsync();
        }

        private static SalesOrderModel CreateOrder(string orderType, string status)
        {
            return new SalesOrderModel
            {
                OrderId = $"ORD-INT-{Guid.NewGuid():N}",
                ProductName = "Integration Test Product",
                Quantity = 10,
                OrderType = orderType,
                Status = status,
                Amount = 150.50m
            };
        }

        private async Task PublishOrderAsync(SalesOrderModel order)
        {
            var message = new ServiceBusMessage(JsonSerializer.Serialize(order))
            {
                MessageId = order.OrderId,
                Subject = "ServiceBusIntegrationTest"
            };

            message.ApplicationProperties["OrderType"] = order.OrderType;
            message.ApplicationProperties["Status"] = order.Status;
            message.ApplicationProperties["OrderId"] = order.OrderId;

            await _sender.SendMessageAsync(message);
            await Task.Delay(DeliveryWaitMilliseconds);
        }

        private async Task<bool> SubscriptionHasOrderAsync(string subscriptionName, string orderId)
        {
            await using var receiver = _client.CreateReceiver(TopicName, subscriptionName);

            long? fromSequenceNumber = null;

            for (var page = 0; page < 10; page++)
            {
                IReadOnlyList<ServiceBusReceivedMessage> messages = fromSequenceNumber.HasValue
                    ? await receiver.PeekMessagesAsync(maxMessages: 50, fromSequenceNumber: fromSequenceNumber.Value)
                    : await receiver.PeekMessagesAsync(maxMessages: 50);

                if (messages.Count == 0)
                    return false;

                if (messages.Any(message => IsMatchingOrder(message, orderId)))
                    return true;

                fromSequenceNumber = messages[^1].SequenceNumber + 1;
            }

            return false;
        }

        private static bool IsMatchingOrder(ServiceBusReceivedMessage message, string orderId)
        {
            if (message.MessageId == orderId)
                return true;

            if (message.ApplicationProperties.TryGetValue("OrderId", out var propertyOrderId)
                && string.Equals(propertyOrderId?.ToString(), orderId, StringComparison.Ordinal))
            {
                return true;
            }

            using var doc = JsonDocument.Parse(message.Body);
            var root = doc.RootElement;

            if (root.TryGetProperty("OrderId", out var pascalOrderId))
                return pascalOrderId.GetString() == orderId;

            if (root.TryGetProperty("orderId", out var camelOrderId))
                return camelOrderId.GetString() == orderId;

            return false;
        }

        private static string GetServiceBusConnectionString()
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<ServiceBusIntegrationTests>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("ServiceBus")
                ?? configuration["ConnectionStrings:ServiceBus"]
                ?? configuration["SERVICEBUS_CONNECTION_STRING"]
                ?? configuration["AZURE_SERVICEBUS_CONNECTION_STRING"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "A real Service Bus connection string is required. Set it in user secrets as " +
                    "'ConnectionStrings:ServiceBus' or in an environment variable named " +
                    "'SERVICEBUS_CONNECTION_STRING'.");
            }

            return connectionString;
        }
    }
}
