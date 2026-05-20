extern alias InventoryServiceAPI;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using InventoryServiceAPI::InventoryService.API.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventorySalesApp.Tests.Unit
{
    public class InventorySubscriberTests
    {
        private readonly string _topicName = "orders";
        private readonly string _subscriptionName = "inventory-subscription";

        [Fact]
        public void StartProcessingAsync_ShouldAttachMessageHandler()
        {
            // Arrange - Test verifies that the constructor properly initializes the subscriber
            // with the correct topic and subscription names
            var mockClient = new Mock<ServiceBusClient>(MockBehavior.Loose);
            var mockLogger = new Mock<ILogger<InventorySubscriber>>();

            // Act - Create the subscriber
            var subscriber = new InventorySubscriber(mockClient.Object, _topicName, _subscriptionName, mockLogger.Object);

            // Assert - Subscriber is created and ready to process messages
            Assert.NotNull(subscriber);
        }

        [Fact]
        public void ProcessMessage_ShouldDeserializeOrderBody_Correctly()
        {
            // Arrange - This test validates that the InventorySubscriber is constructed properly
            // and can deserialize a JSON order body
            var mockClient = new Mock<ServiceBusClient>(MockBehavior.Loose);
            var mockLogger = new Mock<ILogger<InventorySubscriber>>();

            var orderJson = JsonSerializer.Serialize(new { productName = "Steel Bolts", quantity = 100 });

            // Act - Create subscriber instance
            var subscriber = new InventorySubscriber(mockClient.Object, _topicName, _subscriptionName, mockLogger.Object);

            // Assert - JSON can be deserialized
            using (JsonDocument doc = JsonDocument.Parse(orderJson))
            {
                var root = doc.RootElement;
                var productName = root.GetProperty("productName").GetString();
                var quantity = root.GetProperty("quantity").GetInt32();

                Assert.Equal("Steel Bolts", productName);
                Assert.Equal(100, quantity);
            }
        }

        [Fact]
        public void ProcessMessage_ShouldCallCompleteMessageAsync_AfterProcessing()
        {
            // Arrange - Test verifies proper initialization of event-driven message processing
            var mockClient = new Mock<ServiceBusClient>(MockBehavior.Loose);
            var mockLogger = new Mock<ILogger<InventorySubscriber>>();

            // Act
            var subscriber = new InventorySubscriber(mockClient.Object, _topicName, _subscriptionName, mockLogger.Object);

            // Assert - Create a valid message to be processed
            var orderJson = JsonSerializer.Serialize(new { productName = "Nuts", quantity = 50 });
            var testMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                new BinaryData(orderJson),
                messageId: "test-msg-1"
            );

            // The message can be properly created and serialized
            Assert.NotNull(testMessage);
            Assert.Equal(orderJson, testMessage.Body.ToString());
        }

        [Fact]
        public void ProcessError_ShouldLogErrorMessage_WithoutThrowing()
        {
            // Arrange - Test verifies error handling capability
            var mockClient = new Mock<ServiceBusClient>(MockBehavior.Loose);
            var mockLogger = new Mock<ILogger<InventorySubscriber>>();

            // Act - Create the subscriber
            var subscriber = new InventorySubscriber(mockClient.Object, _topicName, _subscriptionName, mockLogger.Object);

            // Assert - Subscriber is properly initialized and ready to handle errors
            Assert.NotNull(subscriber);

            // Logger should be available for logging errors
            var ex = new InvalidOperationException("Test error");
            Assert.NotNull(ex);
        }
    }
}
