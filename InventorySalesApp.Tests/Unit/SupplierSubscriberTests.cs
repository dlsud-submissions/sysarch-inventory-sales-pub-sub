extern alias SupplierServiceAPI;

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Moq;
using SupplierServiceAPI::SupplierService.API.Services;
using Xunit;

namespace InventorySalesApp.Tests.Unit
{
    public class SupplierSubscriberTests
    {
        private const string TopicName = "orders";
        private const string SubscriptionName = "supplier-subscription";

        [Fact]
        public async Task StartProcessingAsync_ShouldAttachMessageHandler()
        {
            var mockClient = new Mock<ServiceBusClient>(MockBehavior.Loose);
            var mockProcessor = new Mock<ServiceBusProcessor>(MockBehavior.Loose);
            var mockLogger = new Mock<ILogger<SupplierSubscriber>>();

            mockProcessor
                .Setup(p => p.StartProcessingAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var subscriber = new SupplierSubscriber(
                mockClient.Object,
                TopicName,
                SubscriptionName,
                mockLogger.Object,
                () => mockProcessor.Object);

            await subscriber.StartProcessingAsync();

            mockProcessor.Verify(p => p.StartProcessingAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessMessage_ShouldDeserializeOrderBody_Correctly()
        {
            var subscriber = CreateSubscriber();
            var message = CreateMessage(new
            {
                productName = "Steel Bolts",
                quantity = 100,
                status = "LowStock"
            });

            await subscriber.ProcessReceivedMessageAsync(message, _ => Task.CompletedTask);

            var alert = Assert.Single(subscriber.GetReorderAlerts());
            Assert.Equal("Steel Bolts", alert.ProductName);
            Assert.Equal(100, alert.QuantityNeeded);
            Assert.True((DateTime.UtcNow - alert.AlertTimestamp).TotalSeconds < 5);
        }

        [Fact]
        public async Task ProcessMessage_ShouldCallCompleteMessageAsync_AfterProcessing()
        {
            var subscriber = CreateSubscriber();
            var message = CreateMessage(new
            {
                ProductName = "Machine Screws",
                Quantity = 50,
                Status = "LowStock"
            });

            var completeCalled = false;

            await subscriber.ProcessReceivedMessageAsync(message, _ =>
            {
                completeCalled = true;
                return Task.CompletedTask;
            });

            Assert.True(completeCalled);
            Assert.Equal("Machine Screws", subscriber.GetReorderAlerts().Single().ProductName);
        }

        [Fact]
        public async Task ProcessError_ShouldLogErrorMessage_WithoutThrowing()
        {
            var subscriber = CreateSubscriber(out var mockLogger);
            var args = new ProcessErrorEventArgs(
                new InvalidOperationException("Test error"),
                ServiceBusErrorSource.Receive,
                "orders",
                "supplier-subscription",
                CancellationToken.None);

            await subscriber.ProcessErrorForTestAsync(args);

            mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((_, _) => true),
                    It.IsAny<InvalidOperationException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static SupplierSubscriber CreateSubscriber()
        {
            return CreateSubscriber(out _);
        }

        private static SupplierSubscriber CreateSubscriber(out Mock<ILogger<SupplierSubscriber>> mockLogger)
        {
            var mockClient = new Mock<ServiceBusClient>(MockBehavior.Loose);
            mockLogger = new Mock<ILogger<SupplierSubscriber>>();

            return new SupplierSubscriber(
                mockClient.Object,
                TopicName,
                SubscriptionName,
                mockLogger.Object);
        }

        private static ServiceBusReceivedMessage CreateMessage(object order)
        {
            return ServiceBusModelFactory.ServiceBusReceivedMessage(
                new BinaryData(JsonSerializer.Serialize(order)),
                messageId: Guid.NewGuid().ToString());
        }
    }
}
