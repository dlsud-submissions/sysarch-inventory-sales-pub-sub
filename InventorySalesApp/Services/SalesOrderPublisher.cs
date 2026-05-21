using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using InventorySalesApp.Models;
using Microsoft.Extensions.Configuration;

namespace InventorySalesApp.Services
{
    public class SalesOrderPublisher : ISalesOrderPublisher
    {
        private readonly ServiceBusClient _client;
        private readonly string _topicName;

        // Production constructor - reads from configuration
        public SalesOrderPublisher(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("ServiceBus") ?? configuration["ConnectionStrings:ServiceBus"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ServiceBus connection string is not configured");
            }

            _topicName = configuration["ServiceBus:TopicName"] ?? "orders";
            _client = new ServiceBusClient(connectionString);
        }

        // Constructor for unit tests to inject mocks
        public SalesOrderPublisher(ServiceBusClient client, string topicName)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _topicName = topicName ?? throw new ArgumentNullException(nameof(topicName));
        }

        public virtual async Task PublishOrderAsync(SalesOrderModel order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            var sender = _client.CreateSender(_topicName);
            var json = JsonSerializer.Serialize(order);
            var message = new ServiceBusMessage(json);

            // Set application properties for SQL filtering
            message.ApplicationProperties["OrderType"] = order.OrderType;
            message.ApplicationProperties["Status"] = order.Status;

            await sender.SendMessageAsync(message).ConfigureAwait(false);
        }
    }
}
