extern alias SupplierServiceAPI;

using System.Net;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using SupplierServiceAPI::SupplierService.API.Services;
using Xunit;

namespace InventorySalesApp.Tests.Integration
{
    public class StubSupplierSubscriber : SupplierSubscriber
    {
        public StubSupplierSubscriber()
            : base(
                client: new ServiceBusClient("Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=ZmFrZWtleWZha2VrZXlmYWtla2V5ZmFrZWtleWZha2U="),
                topicName: "orders",
                subscriptionName: "supplier-subscription",
                logger: NullLogger<SupplierSubscriber>.Instance)
        {
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }

    public class SupplierApiIntegrationTests : IAsyncLifetime
    {
        private WebApplicationFactory<SupplierServiceAPI::Program> _factory = null!;
        private HttpClient _client = null!;

        public async Task InitializeAsync()
        {
            _factory = new WebApplicationFactory<SupplierServiceAPI::Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        var subscriberDescriptors = services
                            .Where(d => d.ServiceType == typeof(SupplierSubscriber))
                            .ToList();
                        foreach (var descriptor in subscriberDescriptors)
                            services.Remove(descriptor);

                        var hostedDescriptors = services
                            .Where(d => d.ServiceType == typeof(IHostedService))
                            .ToList();
                        foreach (var descriptor in hostedDescriptors)
                            services.Remove(descriptor);

                        services.AddSingleton<SupplierSubscriber, StubSupplierSubscriber>();
                        services.AddHostedService(sp => sp.GetRequiredService<SupplierSubscriber>());
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
        public async Task GET_Supplier_ShouldReturn200()
        {
            var response = await _client.GetAsync("/api/supplier");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GET_Supplier_ShouldReturn_ServiceRunningMessage()
        {
            var response = await _client.GetAsync("/api/supplier");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content);

            using var doc = JsonDocument.Parse(content);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        }
    }
}
