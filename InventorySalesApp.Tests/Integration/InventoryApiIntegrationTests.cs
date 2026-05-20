extern alias InventoryServiceAPI;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Text.Json;
using Xunit;

using InventorySubscriber = InventoryServiceAPI::InventoryService.API.Services.InventorySubscriber;

namespace InventorySalesApp.Tests.Integration
{
    /// <summary>
    /// Stub subscriber that skips Azure connection — used only in integration tests.
    /// Inherits InventorySubscriber but overrides ExecuteAsync to do nothing.
    /// </summary>
    public class StubInventorySubscriber : InventorySubscriber
    {
        // Use the test constructor that accepts injected mocks — pass a fake client
        // We can't use the production constructor (needs real Azure connection string),
        // so we reuse the test constructor with a Mock ServiceBusClient.
        // Since ExecuteAsync is overridden to do nothing, no real connection is made.
        public StubInventorySubscriber()
            : base(
                client: new Azure.Messaging.ServiceBus.ServiceBusClient("Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=ZmFrZWtleWZha2VrZXlmYWtla2V5ZmFrZWtleWZha2U="),
                topicName: "orders",
                subscriptionName: "inventory-subscription",
                logger: NullLogger<InventorySubscriber>.Instance)
        {
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // No-op: don't connect to Azure in tests
            return Task.CompletedTask;
        }
    }

    public class InventoryApiIntegrationTests : IAsyncLifetime
    {
        private WebApplicationFactory<InventoryServiceAPI::Program> _factory = null!;
        private HttpClient _client = null!;

        public async Task InitializeAsync()
        {
            _factory = new WebApplicationFactory<InventoryServiceAPI::Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        // Remove the real InventorySubscriber singleton
                        var subscriberDesc = services
                            .Where(d => d.ServiceType == typeof(InventorySubscriber))
                            .ToList();
                        foreach (var d in subscriberDesc)
                            services.Remove(d);

                        // Remove hosted service wrappers that reference InventorySubscriber
                        var hostedDescs = services
                            .Where(d => d.ServiceType == typeof(IHostedService))
                            .ToList();
                        foreach (var d in hostedDescs)
                            services.Remove(d);

                        // Register the stub as singleton (satisfies both controller DI and hosted service)
                        services.AddSingleton<InventorySubscriber, StubInventorySubscriber>();
                        services.AddHostedService(sp => sp.GetRequiredService<InventorySubscriber>());
                    });
                });

            _client = _factory.CreateClient();
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _client?.Dispose();
            _factory?.Dispose();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task GET_Inventory_ShouldReturn200()
        {
            var response = await _client.GetAsync("/api/inventory");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GET_Inventory_ShouldReturn_ServiceRunningMessage()
        {
            var response = await _client.GetAsync("/api/inventory");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content);

            // Response must be a valid JSON array (empty or with items)
            using var doc = JsonDocument.Parse(content);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        }
    }
}