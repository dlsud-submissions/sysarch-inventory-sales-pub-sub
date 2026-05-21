using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using InventorySalesApp.Services;
using Moq;

namespace InventorySalesApp.Tests.Unit
{
    public class SubscriptionStatsServiceTests
    {
        private const string TopicName = "orders";

        [Fact]
        public async Task GetAllSubscriptionStatsAsync_ShouldReturnThreeSubscriptions()
        {
            var mockAdminClient = CreateAdminClientMock();
            SetupRuntimeProperties(mockAdminClient, "inventory-subscription", activeMessageCount: 1);
            SetupRuntimeProperties(mockAdminClient, "accounting-subscription", activeMessageCount: 2);
            SetupRuntimeProperties(mockAdminClient, "supplier-subscription", activeMessageCount: 3);
            var service = new SubscriptionStatsService(mockAdminClient.Object, TopicName);

            var results = await service.GetAllSubscriptionStatsAsync();

            Assert.Equal(3, results.Count);
            Assert.Contains(results, stat => stat.SubscriptionName == "inventory-subscription");
            Assert.Contains(results, stat => stat.SubscriptionName == "accounting-subscription");
            Assert.Contains(results, stat => stat.SubscriptionName == "supplier-subscription");
        }

        [Fact]
        public async Task GetAllSubscriptionStatsAsync_ShouldMapActiveMessageCount_Correctly()
        {
            var mockAdminClient = CreateAdminClientMock();
            SetupRuntimeProperties(mockAdminClient, "inventory-subscription", activeMessageCount: 9);
            SetupRuntimeProperties(mockAdminClient, "accounting-subscription");
            SetupRuntimeProperties(mockAdminClient, "supplier-subscription");
            var service = new SubscriptionStatsService(mockAdminClient.Object, TopicName);

            var results = await service.GetAllSubscriptionStatsAsync();

            var inventoryStats = Assert.Single(results, stat => stat.SubscriptionName == "inventory-subscription");
            Assert.Equal(9, inventoryStats.ActiveMessageCount);
            Assert.Equal("Active", inventoryStats.Status);
        }

        [Fact]
        public async Task GetAllSubscriptionStatsAsync_ShouldMapDeadLetterCount_Correctly()
        {
            var mockAdminClient = CreateAdminClientMock();
            SetupRuntimeProperties(mockAdminClient, "inventory-subscription");
            SetupRuntimeProperties(mockAdminClient, "accounting-subscription", deadLetterMessageCount: 4);
            SetupRuntimeProperties(mockAdminClient, "supplier-subscription");
            var service = new SubscriptionStatsService(mockAdminClient.Object, TopicName);

            var results = await service.GetAllSubscriptionStatsAsync();

            var accountingStats = Assert.Single(results, stat => stat.SubscriptionName == "accounting-subscription");
            Assert.Equal(4, accountingStats.DeadLetterMessageCount);
        }

        [Fact]
        public async Task GetAllSubscriptionStatsAsync_ShouldSetLastRefreshed_ToApproximatelyNow()
        {
            var mockAdminClient = CreateAdminClientMock();
            SetupRuntimeProperties(mockAdminClient, "inventory-subscription");
            SetupRuntimeProperties(mockAdminClient, "accounting-subscription");
            SetupRuntimeProperties(mockAdminClient, "supplier-subscription");
            var service = new SubscriptionStatsService(mockAdminClient.Object, TopicName);
            var before = DateTime.UtcNow;

            var results = await service.GetAllSubscriptionStatsAsync();

            var after = DateTime.UtcNow;
            Assert.All(results, stat =>
            {
                Assert.True(stat.LastRefreshed >= before);
                Assert.True(stat.LastRefreshed <= after);
            });
        }

        [Fact]
        public async Task GetAllSubscriptionStatsAsync_ShouldHandleAdminClientFailure_Gracefully()
        {
            var mockAdminClient = CreateAdminClientMock();
            SetupRuntimeProperties(mockAdminClient, "inventory-subscription", activeMessageCount: 7);
            SetupRuntimeFailure(mockAdminClient, "accounting-subscription");
            SetupRuntimeProperties(mockAdminClient, "supplier-subscription", activeMessageCount: 2);
            var service = new SubscriptionStatsService(mockAdminClient.Object, TopicName);

            var results = await service.GetAllSubscriptionStatsAsync();

            Assert.Equal(2, results.Count);
            Assert.Contains(results, stat => stat.SubscriptionName == "inventory-subscription");
            Assert.Contains(results, stat => stat.SubscriptionName == "supplier-subscription");
            Assert.DoesNotContain(results, stat => stat.SubscriptionName == "accounting-subscription");
        }

        private static Mock<ServiceBusAdministrationClient> CreateAdminClientMock()
        {
            return new Mock<ServiceBusAdministrationClient>(MockBehavior.Strict);
        }

        private static void SetupRuntimeProperties(
            Mock<ServiceBusAdministrationClient> mockAdminClient,
            string subscriptionName,
            long activeMessageCount = 0,
            long deadLetterMessageCount = 0,
            long transferMessageCount = 0,
            long totalMessageCount = 0)
        {
            var now = DateTimeOffset.UtcNow;
            var runtimeProperties = ServiceBusModelFactory.SubscriptionRuntimeProperties(
                TopicName,
                subscriptionName,
                activeMessageCount,
                deadLetterMessageCount,
                transferDeadLetterMessageCount: 0,
                transferMessageCount,
                totalMessageCount,
                createdAt: now,
                updatedAt: now,
                accessedAt: now);

            mockAdminClient
                .Setup(client => client.GetSubscriptionRuntimePropertiesAsync(
                    TopicName,
                    subscriptionName,
                    default))
                .ReturnsAsync(Response.FromValue(runtimeProperties, Mock.Of<Response>()));
        }

        private static void SetupRuntimeFailure(
            Mock<ServiceBusAdministrationClient> mockAdminClient,
            string subscriptionName)
        {
            mockAdminClient
                .Setup(client => client.GetSubscriptionRuntimePropertiesAsync(
                    TopicName,
                    subscriptionName,
                    default))
                .ThrowsAsync(new RequestFailedException("subscription unavailable"));
        }
    }
}
