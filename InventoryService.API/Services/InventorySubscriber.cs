using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using InventoryService.API.Models;
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
        private ServiceBusProcessor? _processor;

        // In-memory stock store: ProductName -> InventoryItem
        private readonly ConcurrentDictionary<string, InventoryItem> _inventory = new();

        // Production constructor - reads from configuration
        public InventorySubscriber(IConfiguration configuration, ILogger<InventorySubscriber> logger)
        {
            var connectionString = configuration["ServiceBus:ConnectionString"]
                ?? configuration.GetConnectionString("ServiceBus");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("ServiceBus connection string is not configured");

            _topicName = configuration["ServiceBus:TopicName"] ?? "orders";
            _subscriptionName = configuration["ServiceBus:SubscriptionName"] ?? "inventory-subscription";
            _client = new ServiceBusClient(connectionString);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Constructor for unit tests to inject mocks
        public InventorySubscriber(
            ServiceBusClient client,
            string topicName,
            string subscriptionName,
            ILogger<InventorySubscriber> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _topicName = topicName ?? throw new ArgumentNullException(nameof(topicName));
            _subscriptionName = subscriptionName ?? throw new ArgumentNullException(nameof(subscriptionName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Returns the current in-memory inventory list.
        /// </summary>
        public IEnumerable<InventoryItem> GetInventory() => _inventory.Values;

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

                _processor.ProcessMessageAsync += ProcessMessageAsync;
                _processor.ProcessErrorAsync += ProcessErrorAsync;

                await _processor.StartProcessingAsync(stoppingToken).ConfigureAwait(false);

                _logger.LogInformation("[Inventory] Subscriber is listening...");

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
        /// Processes an incoming message — updates in-memory inventory.
        /// </summary>
        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            try
            {
                var body = args.Message.Body.ToString();
                _logger.LogInformation("[Inventory] Received: {message}", body);

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                // Support both camelCase and PascalCase property names
                var productName = root.TryGetProperty("ProductName", out var pn)
                    ? pn.GetString()
                    : root.TryGetProperty("productName", out var pn2)
                        ? pn2.GetString()
                        : null;

                var quantity = root.TryGetProperty("Quantity", out var q)
                    ? q.GetInt32()
                    : root.TryGetProperty("quantity", out var q2)
                        ? q2.GetInt32()
                        : 0;

                if (!string.IsNullOrWhiteSpace(productName))
                {
                    _inventory.AddOrUpdate(
                        productName,
                        _ => new InventoryItem { ProductName = productName!, Quantity = quantity, LastUpdated = DateTime.UtcNow },
                        (_, existing) =>
                        {
                            existing.Quantity = quantity;
                            existing.LastUpdated = DateTime.UtcNow;
                            return existing;
                        });

                    _logger.LogDebug("[Inventory] Updated stock: {product} = {qty}", productName, quantity);
                }

                await args.CompleteMessageAsync(args.Message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Inventory] Error processing message");
                await args.AbandonMessageAsync(args.Message).ConfigureAwait(false);
            }
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "[Inventory] Error in message processor: {errorSource}", args.ErrorSource);
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_processor != null)
                await _processor.StopProcessingAsync(cancellationToken).ConfigureAwait(false);

            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}