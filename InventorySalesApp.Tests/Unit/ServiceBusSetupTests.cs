using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using Azure;
using InventorySalesApp.Services;
using Moq;
using Xunit;

namespace InventorySalesApp.Tests.Unit
{
    public class ServiceBusSetupTests
    {
        [Fact]
        public async Task ConfigureFiltersAsync_ShouldDeleteDefaultRule_ForAccountingSubscription()
        {
            var topic = "orders";
            var mockAdmin = new Mock<ServiceBusAdministrationClient>(MockBehavior.Loose);

            mockAdmin.Setup(a => a.DeleteRuleAsync(topic, "accounting-subscription", "$Default", default))
                     .Returns(Task.FromResult(Mock.Of<Response>()));
            mockAdmin.Setup(a => a.CreateRuleAsync(topic, "accounting-subscription", It.Is<CreateRuleOptions>(r => r.Name == "PrepaidOrdersOnly"), default))
                     .Returns(Task.FromResult(Mock.Of<Response<RuleProperties>>()));

            var setup = new ServiceBusSetup(mockAdmin.Object, topic);

            await setup.ConfigureFiltersAsync();

            // Verify delete was called for accounting subscription
            mockAdmin.Verify(a => a.DeleteRuleAsync(topic, "accounting-subscription", "$Default", default), Times.Once());
        }

        [Fact]
        public async Task ConfigureFiltersAsync_ShouldCreatePrepaidOrdersOnlyRule()
        {
            var topic = "orders";
            var mockAdmin = new Mock<ServiceBusAdministrationClient>(MockBehavior.Loose);

            mockAdmin.Setup(a => a.DeleteRuleAsync(topic, "accounting-subscription", "$Default", default))
                     .ThrowsAsync(new Azure.RequestFailedException("Not found"));
            mockAdmin.Setup(a => a.CreateRuleAsync(topic, "accounting-subscription", It.Is<CreateRuleOptions>(r => r.Name == "PrepaidOrdersOnly"), default))
                     .Returns(Task.FromResult(Mock.Of<Response<RuleProperties>>()));

            var setup = new ServiceBusSetup(mockAdmin.Object, topic);

            await setup.ConfigureFiltersAsync();

            // Verify create was called for accounting subscription with PrepaidOrdersOnly rule
            mockAdmin.Verify(a => a.CreateRuleAsync(topic, "accounting-subscription", It.Is<CreateRuleOptions>(r => r.Name == "PrepaidOrdersOnly"), default), Times.Once());
        }

        [Fact]
        public async Task ConfigureFiltersAsync_ShouldDeleteDefaultRule_ForSupplierSubscription()
        {
            var topic = "orders";
            var mockAdmin = new Mock<ServiceBusAdministrationClient>(MockBehavior.Loose);

            mockAdmin.Setup(a => a.DeleteRuleAsync(topic, "supplier-subscription", "$Default", default))
                     .Returns(Task.FromResult(Mock.Of<Response>()));
            mockAdmin.Setup(a => a.CreateRuleAsync(topic, "supplier-subscription", It.Is<CreateRuleOptions>(r => r.Name == "LowStockOnly"), default))
                     .Returns(Task.FromResult(Mock.Of<Response<RuleProperties>>()));

            var setup = new ServiceBusSetup(mockAdmin.Object, topic);

            await setup.ConfigureFiltersAsync();

            // Verify delete was called for supplier subscription
            mockAdmin.Verify(a => a.DeleteRuleAsync(topic, "supplier-subscription", "$Default", default), Times.Once());
        }

        [Fact]
        public async Task ConfigureFiltersAsync_ShouldCreateLowStockOnlyRule()
        {
            var topic = "orders";
            var mockAdmin = new Mock<ServiceBusAdministrationClient>(MockBehavior.Loose);

            mockAdmin.Setup(a => a.DeleteRuleAsync(topic, "supplier-subscription", "$Default", default))
                     .ThrowsAsync(new Azure.RequestFailedException("Not found"));
            mockAdmin.Setup(a => a.CreateRuleAsync(topic, "supplier-subscription", It.Is<CreateRuleOptions>(r => r.Name == "LowStockOnly"), default))
                     .Returns(Task.FromResult(Mock.Of<Response<RuleProperties>>()));

            var setup = new ServiceBusSetup(mockAdmin.Object, topic);

            await setup.ConfigureFiltersAsync();

            // Verify create was called for supplier subscription with LowStockOnly rule
            mockAdmin.Verify(a => a.CreateRuleAsync(topic, "supplier-subscription", It.Is<CreateRuleOptions>(r => r.Name == "LowStockOnly"), default), Times.Once());
        }

        [Fact]
        public async Task ConfigureFiltersAsync_ShouldCreateAllOrdersRule_ForInventorySubscription()
        {
            var topic = "orders";
            var mockAdmin = new Mock<ServiceBusAdministrationClient>(MockBehavior.Loose);

            mockAdmin.Setup(a => a.CreateRuleAsync(topic, "inventory-subscription", It.Is<CreateRuleOptions>(r => r.Name == "AllOrders"), default))
                     .Returns(Task.FromResult(Mock.Of<Response<RuleProperties>>()));
            mockAdmin.Setup(a => a.DeleteRuleAsync(topic, "accounting-subscription", "$Default", default))
                     .Returns(Task.FromResult(Mock.Of<Response>()));
            mockAdmin.Setup(a => a.CreateRuleAsync(topic, "accounting-subscription", It.Is<CreateRuleOptions>(r => r.Name == "PrepaidOrdersOnly"), default))
                     .Returns(Task.FromResult(Mock.Of<Response<RuleProperties>>()));
            mockAdmin.Setup(a => a.DeleteRuleAsync(topic, "supplier-subscription", "$Default", default))
                     .Returns(Task.FromResult(Mock.Of<Response>()));
            mockAdmin.Setup(a => a.CreateRuleAsync(topic, "supplier-subscription", It.Is<CreateRuleOptions>(r => r.Name == "LowStockOnly"), default))
                     .Returns(Task.FromResult(Mock.Of<Response<RuleProperties>>()));

            var setup = new ServiceBusSetup(mockAdmin.Object, topic);

            await setup.ConfigureFiltersAsync();

            mockAdmin.Verify(a => a.CreateRuleAsync(topic, "inventory-subscription", It.Is<CreateRuleOptions>(r => r.Name == "AllOrders"), default), Times.Once());
            mockAdmin.Verify(a => a.DeleteRuleAsync(topic, "inventory-subscription", It.IsAny<string>(), default), Times.Never());
        }
    }
}

