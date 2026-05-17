using InventorySalesApp.Models;
using System.ComponentModel.DataAnnotations;

namespace InventorySalesApp.Tests.Unit
{
    public class SalesOrderModelTests
    {
        [Fact]
        public void SalesOrderModel_ShouldHaveOrderIdProperty_GetAndSet()
        {
            // Arrange
            var model = new SalesOrderModel();
            var testOrderId = "ORD-001";

            // Act
            model.OrderId = testOrderId;

            // Assert
            Assert.Equal(testOrderId, model.OrderId);
        }

        [Fact]
        public void SalesOrderModel_ShouldHaveProductNameProperty_GetAndSet()
        {
            // Arrange
            var model = new SalesOrderModel();
            var testProductName = "Steel Bolts";

            // Act
            model.ProductName = testProductName;

            // Assert
            Assert.Equal(testProductName, model.ProductName);
        }

        [Fact]
        public void SalesOrderModel_ShouldHaveQuantityProperty_GetAndSet()
        {
            // Arrange
            var model = new SalesOrderModel();
            var testQuantity = 100;

            // Act
            model.Quantity = testQuantity;

            // Assert
            Assert.Equal(testQuantity, model.Quantity);
        }

        [Fact]
        public void SalesOrderModel_ShouldHaveOrderTypeProperty_GetAndSet()
        {
            // Arrange
            var model = new SalesOrderModel();
            var testOrderType = "Prepaid";

            // Act
            model.OrderType = testOrderType;

            // Assert
            Assert.Equal(testOrderType, model.OrderType);
        }

        [Fact]
        public void SalesOrderModel_ShouldAcceptPrepaidOrderType()
        {
            // Arrange
            var model = new SalesOrderModel();

            // Act
            model.OrderType = "Prepaid";

            // Assert
            Assert.Equal("Prepaid", model.OrderType);
        }

        [Fact]
        public void SalesOrderModel_ShouldAcceptCODOrderType()
        {
            // Arrange
            var model = new SalesOrderModel();

            // Act
            model.OrderType = "COD";

            // Assert
            Assert.Equal("COD", model.OrderType);
        }

        [Fact]
        public void SalesOrderModel_ShouldHaveStatusProperty_GetAndSet()
        {
            // Arrange
            var model = new SalesOrderModel();
            var testStatus = "Pending";

            // Act
            model.Status = testStatus;

            // Assert
            Assert.Equal(testStatus, model.Status);
        }

        [Fact]
        public void SalesOrderModel_ShouldHaveAmountProperty_GetAndSet()
        {
            // Arrange
            var model = new SalesOrderModel();
            var testAmount = 4500.00m;

            // Act
            model.Amount = testAmount;

            // Assert
            Assert.Equal(testAmount, model.Amount);
        }

        [Fact]
        public void SalesOrderModel_ShouldInstantiateWithAllProperties()
        {
            // Arrange & Act
            var model = new SalesOrderModel
            {
                OrderId = "ORD-001",
                ProductName = "Steel Bolts",
                Quantity = 100,
                OrderType = "Prepaid",
                Status = "Pending",
                Amount = 4500.00m
            };

            // Assert
            Assert.Equal("ORD-001", model.OrderId);
            Assert.Equal("Steel Bolts", model.ProductName);
            Assert.Equal(100, model.Quantity);
            Assert.Equal("Prepaid", model.OrderType);
            Assert.Equal("Pending", model.Status);
            Assert.Equal(4500.00m, model.Amount);
        }

        [Fact]
        public void SalesOrderModel_ShouldAllowModificationOfAllProperties()
        {
            // Arrange
            var model = new SalesOrderModel
            {
                OrderId = "ORD-001",
                ProductName = "Steel Bolts",
                Quantity = 100,
                OrderType = "Prepaid",
                Status = "Pending",
                Amount = 4500.00m
            };

            // Act
            model.OrderId = "ORD-002";
            model.ProductName = "Aluminum Rods";
            model.Quantity = 200;
            model.OrderType = "COD";
            model.Status = "Fulfilled";
            model.Amount = 9000.00m;

            // Assert
            Assert.Equal("ORD-002", model.OrderId);
            Assert.Equal("Aluminum Rods", model.ProductName);
            Assert.Equal(200, model.Quantity);
            Assert.Equal("COD", model.OrderType);
            Assert.Equal("Fulfilled", model.Status);
            Assert.Equal(9000.00m, model.Amount);
        }

        [Fact]
        public void SalesOrderModel_ShouldHaveKeyAttribute_OnOrderId()
        {
            // Arrange
            var property = typeof(SalesOrderModel).GetProperty(nameof(SalesOrderModel.OrderId));

            // Act & Assert
            Assert.NotNull(property);
            Assert.True(property.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0,
                "OrderId should have [Key] attribute");
        }

        [Fact]
        public void SalesOrderModel_ShouldValidateOrderType_WithDataAnnotations()
        {
            // Arrange
            var model = new SalesOrderModel
            {
                OrderId = "ORD-001",
                ProductName = "Test Product",
                Quantity = 10,
                OrderType = "Prepaid",
                Status = "Pending",
                Amount = 100m
            };
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();

            // Act & Assert - Valid OrderType should pass
            var isValid = Validator.TryValidateObject(model, context, results, true);
            Assert.True(isValid, "Model with valid OrderType should pass validation");
        }

        [Fact]
        public void SalesOrderModel_ShouldValidateStatus_WithDataAnnotations()
        {
            // Arrange
            var model = new SalesOrderModel
            {
                OrderId = "ORD-001",
                ProductName = "Test Product",
                Quantity = 10,
                OrderType = "Prepaid",
                Status = "Fulfilled",
                Amount = 100m
            };
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();

            // Act & Assert - Valid Status should pass
            var isValid = Validator.TryValidateObject(model, context, results, true);
            Assert.True(isValid, "Model with valid Status should pass validation");
        }

        [Fact]
        public void SalesOrderModel_ShouldValidateQuantity_MustBeGreaterThanZero()
        {
            // Arrange
            var model = new SalesOrderModel
            {
                OrderId = "ORD-001",
                ProductName = "Test Product",
                Quantity = 0,
                OrderType = "Prepaid",
                Status = "Pending",
                Amount = 100m
            };
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(model, context, results, true);

            // Assert - Quantity of 0 should fail validation
            Assert.False(isValid, "Model with Quantity of 0 should fail validation");
            Assert.Single(results);
            Assert.Contains("Quantity must be greater than 0", results[0].ErrorMessage);
        }

        [Fact]
        public void SalesOrderModel_ShouldValidateAmount_CanBeZero()
        {
            // Arrange
            var model = new SalesOrderModel
            {
                OrderId = "ORD-001",
                ProductName = "Test Product",
                Quantity = 10,
                OrderType = "Prepaid",
                Status = "Pending",
                Amount = 0m
            };
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();

            // Act & Assert - Amount of 0 should be valid
            var isValid = Validator.TryValidateObject(model, context, results, true);
            Assert.True(isValid, "Model with Amount of 0 should pass validation");
        }
    }
}
