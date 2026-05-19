using InventorySalesApp.Controllers;
using InventorySalesApp.Models;
using InventorySalesApp.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace InventorySalesApp.Tests.Unit
{
    public class SalesOrderControllerTests
    {
        [Fact]
        public void PlaceOrder_GET_ShouldReturnPlaceOrderView()
        {
            // Arrange
            var mockPublisher = new Mock<ISalesOrderPublisher>();
            var controller = new SalesOrderController(mockPublisher.Object);

            // Act
            var result = controller.PlaceOrder();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            // ViewName is null when using default view name, which is correct for PlaceOrder action
            Assert.IsType<SalesOrderModel>(viewResult.Model);
        }

        [Fact]
        public async Task PlaceOrder_POST_ShouldCallPublisher_WhenModelIsValid()
        {
            // Arrange
            var mockPublisher = new Mock<ISalesOrderPublisher>();
            mockPublisher
                .Setup(p => p.PublishOrderAsync(It.IsAny<SalesOrderModel>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var controller = new SalesOrderController(mockPublisher.Object);

            var model = new SalesOrderModel
            {
                OrderId = "ORD-001",
                ProductName = "Test Product",
                Quantity = 5,
                OrderType = "Prepaid",
                Status = "Pending",
                Amount = 100m
            };

            // Act
            var result = await controller.PlaceOrder(model);

            // Assert
            mockPublisher.Verify(p => p.PublishOrderAsync(It.Is<SalesOrderModel>(
                m => m.OrderId == "ORD-001" &&
                     m.ProductName == "Test Product" &&
                     m.Quantity == 5 &&
                     m.OrderType == "Prepaid" &&
                     m.Status == "Pending" &&
                     m.Amount == 100m
            )), Times.Once);
        }

        [Fact]
        public async Task PlaceOrder_POST_ShouldReturnOrderConfirmationView_AfterPublish()
        {
            // Arrange
            var mockPublisher = new Mock<ISalesOrderPublisher>();
            mockPublisher
                .Setup(p => p.PublishOrderAsync(It.IsAny<SalesOrderModel>()))
                .Returns(Task.CompletedTask);

            var controller = new SalesOrderController(mockPublisher.Object);

            var model = new SalesOrderModel
            {
                OrderId = "ORD-002",
                ProductName = "Another Product",
                Quantity = 10,
                OrderType = "COD",
                Status = "Fulfilled",
                Amount = 250m
            };

            // Act
            var result = await controller.PlaceOrder(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("OrderConfirmation", redirectResult.ActionName);
            // Controller name may be null since redirect is within the same controller
            Assert.True(redirectResult.ControllerName == "SalesOrder" || redirectResult.ControllerName == null);
        }

        [Fact]
        public async Task PlaceOrder_POST_ShouldReturnPlaceOrderView_WhenModelStateIsInvalid()
        {
            // Arrange
            var mockPublisher = new Mock<ISalesOrderPublisher>();
            var controller = new SalesOrderController(mockPublisher.Object);

            var model = new SalesOrderModel
            {
                OrderId = "",
                ProductName = "",
                Quantity = -1,
                OrderType = "InvalidType",
                Status = "InvalidStatus",
                Amount = -100
            };

            // Add model errors to simulate validation failure
            controller.ModelState.AddModelError("OrderId", "Order ID is required");
            controller.ModelState.AddModelError("ProductName", "Product name is required");

            // Act
            var result = await controller.PlaceOrder(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            // ViewName is null when using default view name, which is correct for PlaceOrder action
            Assert.Equal(model, viewResult.Model);

            // Verify PublishOrderAsync was NOT called for invalid model
            mockPublisher.Verify(
                p => p.PublishOrderAsync(It.IsAny<SalesOrderModel>()),
                Times.Never);
        }

        [Fact]
        public void OrderConfirmation_ShouldReturnViewWithModel()
        {
            // Arrange
            var mockPublisher = new Mock<ISalesOrderPublisher>();
            var controller = new SalesOrderController(mockPublisher.Object);

            var model = new SalesOrderModel
            {
                OrderId = "ORD-003",
                ProductName = "Confirmation Test",
                Quantity = 15,
                OrderType = "Prepaid",
                Status = "LowStock",
                Amount = 500m
            };

            // Act
            var result = controller.OrderConfirmation(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            // ViewName is null when using default view name, which is correct for OrderConfirmation action
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public void OrderConfirmation_ShouldRedirectToPlaceOrder_WhenModelIsNull()
        {
            // Arrange
            var mockPublisher = new Mock<ISalesOrderPublisher>();
            var controller = new SalesOrderController(mockPublisher.Object);

            // Act
            var result = controller.OrderConfirmation(null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("PlaceOrder", redirectResult.ActionName);
        }
    }
}
