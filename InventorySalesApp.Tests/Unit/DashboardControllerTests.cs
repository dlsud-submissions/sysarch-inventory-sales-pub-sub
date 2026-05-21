using InventorySalesApp.Controllers;
using InventorySalesApp.Models;
using InventorySalesApp.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InventorySalesApp.Tests.Unit
{
    public class DashboardControllerTests
    {
        [Fact]
        public async Task Dashboard_GET_ShouldReturnDashboardView()
        {
            var mockStatsService = CreateStatsServiceMock(CreateStats());
            var controller = new DashboardController(mockStatsService.Object);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public async Task Dashboard_GET_ShouldPassSubscriptionStatsToView()
        {
            var stats = CreateStats();
            var mockStatsService = CreateStatsServiceMock(stats);
            var controller = new DashboardController(mockStatsService.Object);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<SubscriptionStatsViewModel>>(viewResult.Model);
            Assert.Same(stats, model);
        }

        [Fact]
        public async Task Dashboard_GET_ShouldReturnThreeSubscriptions_InModel()
        {
            var mockStatsService = CreateStatsServiceMock(CreateStats());
            var controller = new DashboardController(mockStatsService.Object);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<SubscriptionStatsViewModel>>(viewResult.Model);
            Assert.Equal(3, model.Count);
        }

        [Fact]
        public async Task Dashboard_GET_WhenServiceFails_ShouldReturnEmptyList_NotException()
        {
            var mockStatsService = new Mock<ISubscriptionStatsService>();
            mockStatsService
                .Setup(service => service.GetAllSubscriptionStatsAsync())
                .ThrowsAsync(new InvalidOperationException("Service Bus unavailable"));
            var controller = new DashboardController(mockStatsService.Object);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<SubscriptionStatsViewModel>>(viewResult.Model);
            Assert.Empty(model);
            Assert.Equal("Subscription stats are temporarily unavailable.", viewResult.ViewData["DashboardError"]);
        }

        private static Mock<ISubscriptionStatsService> CreateStatsServiceMock(List<SubscriptionStatsViewModel> stats)
        {
            var mockStatsService = new Mock<ISubscriptionStatsService>();
            mockStatsService
                .Setup(service => service.GetAllSubscriptionStatsAsync())
                .ReturnsAsync(stats);

            return mockStatsService;
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
