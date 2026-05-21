using InventorySalesApp.Models;
using InventorySalesApp.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net;

namespace InventorySalesApp.Tests.Integration
{
    public class DashboardIntegrationTests
    {
        [Fact]
        public async Task GET_Dashboard_ShouldReturn200()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/Dashboard");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GET_Dashboard_ShouldContain_SubscriptionNames_InResponse()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/Dashboard");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("inventory-subscription", content);
            Assert.Contains("accounting-subscription", content);
            Assert.Contains("supplier-subscription", content);
        }

        private static WebApplicationFactory<global::Program> CreateFactory()
        {
            return new WebApplicationFactory<global::Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<ISubscriptionStatsService>();

                        var mockStatsService = new Mock<ISubscriptionStatsService>();
                        mockStatsService
                            .Setup(service => service.GetAllSubscriptionStatsAsync())
                            .ReturnsAsync(CreateStats());

                        services.AddSingleton(mockStatsService.Object);
                    });
                });
        }

        private static List<SubscriptionStatsViewModel> CreateStats()
        {
            return
            [
                new SubscriptionStatsViewModel { SubscriptionName = "inventory-subscription", ActiveMessageCount = 9, DeadLetterMessageCount = 4, TransferMessageCount = 0, TotalMessageCount = 13, Status = "Active", LastRefreshed = DateTime.UtcNow },
                new SubscriptionStatsViewModel { SubscriptionName = "accounting-subscription", ActiveMessageCount = 0, DeadLetterMessageCount = 0, TransferMessageCount = 0, TotalMessageCount = 0, Status = "Active", LastRefreshed = DateTime.UtcNow },
                new SubscriptionStatsViewModel { SubscriptionName = "supplier-subscription", ActiveMessageCount = 12, DeadLetterMessageCount = 1, TransferMessageCount = 0, TotalMessageCount = 13, Status = "Active", LastRefreshed = DateTime.UtcNow }
            ];
        }
    }
}
