extern alias AccountingServiceAPI;

using System.Net;
using System.Text.Json;
using AccountingServiceAPI::AccountingService.API.Services;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InventorySalesApp.Tests.Integration
{
    public class StubAccountingSubscriber : AccountingSubscriber
    {
        public StubAccountingSubscriber()
            : base(
                client: new ServiceBusClient("Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=ZmFrZWtleWZha2VrZXlmYWtla2V5ZmFrZWtleWZha2U="),
                topicName: "orders",
                subscriptionName: "accounting-subscription",
                logger: NullLogger<AccountingSubscriber>.Instance)
        {
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }

    public class AccountingApiIntegrationTests : IAsyncLifetime
    {
        private WebApplicationFactory<AccountingServiceAPI::Program> _factory = null!;
        private HttpClient _client = null!;

        public async Task InitializeAsync()
        {
            _factory = new WebApplicationFactory<AccountingServiceAPI::Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        var subscriberDescriptors = services
                            .Where(d => d.ServiceType == typeof(AccountingSubscriber))
                            .ToList();
                        foreach (var descriptor in subscriberDescriptors)
                            services.Remove(descriptor);

                        var hostedDescriptors = services
                            .Where(d => d.ServiceType == typeof(IHostedService))
                            .ToList();
                        foreach (var descriptor in hostedDescriptors)
                            services.Remove(descriptor);

                        services.AddSingleton<AccountingSubscriber, StubAccountingSubscriber>();
                        services.AddHostedService(sp => sp.GetRequiredService<AccountingSubscriber>());
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
        public async Task GET_Accounting_ShouldReturn200()
        {
            var response = await _client.GetAsync("/api/accounting");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GET_Accounting_ShouldReturn_ServiceRunningMessage()
        {
            var response = await _client.GetAsync("/api/accounting");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content);

            using var doc = JsonDocument.Parse(content);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        }
    }
}
