using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SupplierService.API.Models;

namespace SupplierService.API.Services
{
    /// <summary>
    /// Background service that listens to low-stock order messages for suppliers.
    /// </summary>
    public class SupplierSubscriber : BackgroundService
    {
        private readonly ServiceBusClient _client;
        private readonly string _topicName;
        private readonly string _subscriptionName;
        private readonly ILogger<SupplierSubscriber> _logger;
        private readonly Func<ServiceBusProcessor>? _processorFactory;
        private readonly ConcurrentBag<ReorderAlert> _alerts = new();
        private ServiceBusProcessor? _processor;

        public SupplierSubscriber(IConfiguration configuration, ILogger<SupplierSubscriber> logger)
        {
            var connectionString = configuration["ServiceBus:ConnectionString"]
                ?? configuration.GetConnectionString("ServiceBus");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("ServiceBus connection string is not configured");

            _topicName = configuration["ServiceBus:TopicName"] ?? "orders";
            _subscriptionName = configuration["ServiceBus:SubscriptionName"] ?? "supplier-subscription";
            _client = new ServiceBusClient(connectionString);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public SupplierSubscriber(
            ServiceBusClient client,
            string topicName,
            string subscriptionName,
            ILogger<SupplierSubscriber> logger,
            Func<ServiceBusProcessor>? processorFactory = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _topicName = topicName ?? throw new ArgumentNullException(nameof(topicName));
            _subscriptionName = subscriptionName ?? throw new ArgumentNullException(nameof(subscriptionName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _processorFactory = processorFactory;
        }

        public IEnumerable<ReorderAlert> GetReorderAlerts() => _alerts;

        public Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            _processor = _processorFactory?.Invoke()
                ?? _client.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions
                {
                    AutoCompleteMessages = false
                });

            _processor.ProcessMessageAsync += ProcessMessageAsync;
            _processor.ProcessErrorAsync += ProcessErrorAsync;

            _logger.LogInformation("[Supplier] Subscriber is listening...");

            return _processor.StartProcessingAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await StartProcessingAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[Supplier] Subscriber cancellation requested");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Supplier] Unexpected error in subscriber");
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

        public async Task ProcessReceivedMessageAsync(
            ServiceBusReceivedMessage message,
            Func<ServiceBusReceivedMessage, Task> completeMessageAsync,
            Func<ServiceBusReceivedMessage, Task>? abandonMessageAsync = null)
        {
            try
            {
                var body = message.Body.ToString();
                _logger.LogInformation("[Supplier] Received: {message}", body);

                var alert = DeserializeReorderAlert(body);
                _alerts.Add(alert);

                await completeMessageAsync(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Supplier] Error processing message");

                if (abandonMessageAsync != null)
                    await abandonMessageAsync(message).ConfigureAwait(false);
            }
        }

        public Task ProcessErrorForTestAsync(ProcessErrorEventArgs args) => ProcessErrorAsync(args);

        private Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            return ProcessReceivedMessageAsync(
                args.Message,
                message => args.CompleteMessageAsync(message),
                message => args.AbandonMessageAsync(message));
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "[Supplier] Error in message processor: {errorSource}", args.ErrorSource);
            return Task.CompletedTask;
        }

        private static ReorderAlert DeserializeReorderAlert(string body)
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            return new ReorderAlert
            {
                ProductName = GetString(root, "ProductName", "productName"),
                QuantityNeeded = GetInt(root, "Quantity", "quantity"),
                AlertTimestamp = DateTime.UtcNow
            };
        }

        private static string GetString(JsonElement root, string pascalName, string camelName)
        {
            if (root.TryGetProperty(pascalName, out var pascalValue))
                return pascalValue.GetString() ?? string.Empty;

            if (root.TryGetProperty(camelName, out var camelValue))
                return camelValue.GetString() ?? string.Empty;

            return string.Empty;
        }

        private static int GetInt(JsonElement root, string pascalName, string camelName)
        {
            if (root.TryGetProperty(pascalName, out var pascalValue))
                return pascalValue.GetInt32();

            if (root.TryGetProperty(camelName, out var camelValue))
                return camelValue.GetInt32();

            return 0;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_processor != null)
                await _processor.StopProcessingAsync(cancellationToken).ConfigureAwait(false);

            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
