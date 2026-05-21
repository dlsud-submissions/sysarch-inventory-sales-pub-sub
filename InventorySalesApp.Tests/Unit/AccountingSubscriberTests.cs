extern alias AccountingServiceAPI;

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AccountingServiceAPI::AccountingService.API.Services;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventorySalesApp.Tests.Unit
{
    public class AccountingSubscriberTests
    {
        private const string TopicName = "orders";
        private const string SubscriptionName = "accounting-subscription";

        [Fact]
        public async Task StartProcessingAsync_ShouldAttachMessageHandler()
        {
            var mockClient = new Mock<ServiceBusClient>(MockBehavior.Loose);
            var mockProcessor = new Mock<ServiceBusProcessor>(MockBehavior.Loose);
            var mockLogger = new Mock<ILogger<AccountingSubscriber>>();

            mockProcessor
                .Setup(p => p.StartProcessingAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var subscriber = new AccountingSubscriber(
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
                orderId = "ORD-100",
                amount = 4500.00m,
                orderType = "Prepaid"
            });

            await subscriber.ProcessReceivedMessageAsync(message, _ => Task.CompletedTask);

            var transaction = Assert.Single(subscriber.GetTransactions());
            Assert.Equal("ORD-100", transaction.OrderId);
            Assert.Equal(4500.00m, transaction.Amount);
            Assert.Equal("Prepaid", transaction.OrderType);
            Assert.StartsWith("TXN-", transaction.TransactionId);
            Assert.True((DateTime.UtcNow - transaction.Timestamp).TotalSeconds < 5);
        }

        [Fact]
        public async Task ProcessMessage_ShouldCallCompleteMessageAsync_AfterProcessing()
        {
            var subscriber = CreateSubscriber();
            var message = CreateMessage(new
            {
                OrderId = "ORD-101",
                Amount = 2500.50m,
                OrderType = "Prepaid"
            });

            var completeCalled = false;

            await subscriber.ProcessReceivedMessageAsync(message, _ =>
            {
                completeCalled = true;
                return Task.CompletedTask;
            });

            Assert.True(completeCalled);
            Assert.Equal("ORD-101", subscriber.GetTransactions().Single().OrderId);
        }

        [Fact]
        public async Task ProcessError_ShouldLogErrorMessage_WithoutThrowing()
        {
            var subscriber = CreateSubscriber(out var mockLogger);
            var args = new ProcessErrorEventArgs(
                new InvalidOperationException("Test error"),
                ServiceBusErrorSource.Receive,
                "orders",
                "accounting-subscription",
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

        private static AccountingSubscriber CreateSubscriber()
        {
            return CreateSubscriber(out _);
        }

        private static AccountingSubscriber CreateSubscriber(out Mock<ILogger<AccountingSubscriber>> mockLogger)
        {
            var mockClient = new Mock<ServiceBusClient>(MockBehavior.Loose);
            mockLogger = new Mock<ILogger<AccountingSubscriber>>();

            return new AccountingSubscriber(
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
