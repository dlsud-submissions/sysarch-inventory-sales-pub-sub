using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using InventorySalesApp.Models;
using InventorySalesApp.Services;
using Moq;

namespace InventorySalesApp.Tests.Unit
{
    public class SalesOrderPublisherTests
    {
        [Fact]
        public async Task PublishOrderAsync_ShouldSendMessage_WhenOrderIsValid()
        {
            var topicName = "orders";
            var mockClient = new Mock<ServiceBusClient>(MockBehavior.Strict);
            var mockSender = new Mock<ServiceBusSender>(MockBehavior.Strict);

            mockClient.Setup(c => c.CreateSender(topicName)).Returns(mockSender.Object);

            mockSender.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default)).Returns(Task.CompletedTask).Verifiable();

            var publisher = new SalesOrderPublisher(mockClient.Object, topicName);

            var order = new SalesOrderModel
            {
                OrderId = "ORD-001",
                ProductName = "Steel Bolts",
                Quantity = 100,
                OrderType = "Prepaid",
                Status = "Pending",
                Amount = 4500.00m
            };

            await publisher.PublishOrderAsync(order);

            mockSender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Once);
        }

        [Fact]
        public async Task PublishOrderAsync_ShouldSetOrderTypeProperty_OnMessage()
        {
            var topicName = "orders";
            var mockClient = new Mock<ServiceBusClient>(MockBehavior.Strict);
            var mockSender = new Mock<ServiceBusSender>(MockBehavior.Strict);

            ServiceBusMessage capturedMessage = null;
            mockClient.Setup(c => c.CreateSender(topicName)).Returns(mockSender.Object);
            mockSender.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default))
                      .Callback<ServiceBusMessage, System.Threading.CancellationToken>((m, t) => capturedMessage = m)
                      .Returns(Task.CompletedTask).Verifiable();

            var publisher = new SalesOrderPublisher(mockClient.Object, topicName);

            var order = new SalesOrderModel { OrderId = "ORD-002", ProductName = "Nuts", Quantity = 50, OrderType = "COD", Status = "Pending", Amount = 1000m };

            await publisher.PublishOrderAsync(order);

            Assert.NotNull(capturedMessage);
            Assert.True(capturedMessage.ApplicationProperties.ContainsKey("OrderType"));
            Assert.Equal("COD", capturedMessage.ApplicationProperties["OrderType"]);

            mockSender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Once);
        }

        [Fact]
        public async Task PublishOrderAsync_ShouldSetStatusProperty_OnMessage()
        {
            var topicName = "orders";
            var mockClient = new Mock<ServiceBusClient>(MockBehavior.Strict);
            var mockSender = new Mock<ServiceBusSender>(MockBehavior.Strict);

            ServiceBusMessage capturedMessage = null;
            mockClient.Setup(c => c.CreateSender(topicName)).Returns(mockSender.Object);
            mockSender.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default))
                      .Callback<ServiceBusMessage, System.Threading.CancellationToken>((m, t) => capturedMessage = m)
                      .Returns(Task.CompletedTask).Verifiable();

            var publisher = new SalesOrderPublisher(mockClient.Object, topicName);

            var order = new SalesOrderModel { OrderId = "ORD-003", ProductName = "Bolts", Quantity = 10, OrderType = "Prepaid", Status = "LowStock", Amount = 100m };

            await publisher.PublishOrderAsync(order);

            Assert.NotNull(capturedMessage);
            Assert.True(capturedMessage.ApplicationProperties.ContainsKey("Status"));
            Assert.Equal("LowStock", capturedMessage.ApplicationProperties["Status"]);

            mockSender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Once);
        }

        [Fact]
        public void PublishOrderAsync_ShouldThrow_WhenConnectionStringIsEmpty()
        {
            // Simulate missing configuration by calling the production constructor with an empty configuration
            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();

            Assert.Throws<InvalidOperationException>(() => new SalesOrderPublisher(configuration));
        }
    }
}
