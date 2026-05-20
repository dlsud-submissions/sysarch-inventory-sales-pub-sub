using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InventoryService.API.Services
{
    /// <summary>
    /// Background service that subscribes to the inventory-subscription on the orders topic
    /// and processes incoming order messages to update stock levels.
    /// </summary>
    public class InventorySubscriber : BackgroundService
    {
        private readonly ServiceBusClient _client;
        private readonly string _topicName;
        private readonly string _subscriptionName;
        private readonly ILogger<InventorySubscriber> _logger;
        private ServiceBusProcessor _processor;

        // Production constructor - reads from configuration
        public InventorySubscriber(IConfiguration configuration, ILogger<InventorySubscriber> logger)
        {
            var connectionString = configuration["ServiceBus:ConnectionString"] ?? configuration.GetConnectionString("ServiceBus");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ServiceBus connection string is not configured");
            }

            _topicName = configuration["ServiceBus:TopicName"] ?? "orders";
            _subscriptionName = configuration["ServiceBus:SubscriptionName"] ?? "inventory-subscription";
            _client = new ServiceBusClient(connectionString);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Constructor for unit tests to inject mocks
        public InventorySubscriber(ServiceBusClient client, string topicName, string subscriptionName, ILogger<InventorySubscriber> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _topicName = topicName ?? throw new ArgumentNullException(nameof(topicName));
            _subscriptionName = subscriptionName ?? throw new ArgumentNullException(nameof(subscriptionName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Starts the background service and begins listening for messages.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _processor = _client.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions
                {
                    AutoCompleteMessages = false
                });

                // Attach message handler
                _processor.ProcessMessageAsync += ProcessMessageAsync;

                // Attach error handler
                _processor.ProcessErrorAsync += ProcessErrorAsync;

                // Start processing
                await _processor.StartProcessingAsync(stoppingToken).ConfigureAwait(false);

                _logger.LogInformation("[Inventory] Subscriber is listening...");

                // Keep the service running until cancellation is requested
                await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[Inventory] Subscriber cancellation requested");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Inventory] Unexpected error in subscriber");
                throw;
            }
            finally
            {
                if (_processor != null)
                {
                    await _processor.StopProcessingAsync().ConfigureAwait(false);
                    await _processor.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Processes an incoming message from the subscription.
        /// Deserializes the order body and logs the received inventory information.
        /// </summary>
        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            try
            {
                var body = args.Message.Body.ToString();
                _logger.LogInformation("[Inventory] Received: {message}", body);

                // Deserialize to validate structure
                using (JsonDocument doc = JsonDocument.Parse(body))
                {
                    var root = doc.RootElement;
                    var productName = root.GetProperty("productName").GetString();
                    var quantity = root.GetProperty("quantity").GetInt32();

                    _logger.LogDebug("[Inventory] Processing product: {productName}, quantity: {quantity}", productName, quantity);
                }

                // Mark message as processed
                await args.CompleteMessageAsync(args.Message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Inventory] Error processing message");

                // Don't complete the message so it will be retried
                await args.AbandonMessageAsync(args.Message).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Handles errors that occur during message processing.
        /// </summary>
        private Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "[Inventory] Error in message processor: {errorSource}", args.ErrorSource);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the subscriber gracefully when the application shuts down.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_processor != null)
            {
                await _processor.StopProcessingAsync(cancellationToken).ConfigureAwait(false);
            }

            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
