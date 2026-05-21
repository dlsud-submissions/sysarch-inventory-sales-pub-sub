extern alias InventoryServiceAPI;

using InventoryServiceAPI::InventoryService.API.Models;
using System;
using Xunit;

namespace InventorySalesApp.Tests.Unit
{
    public class InventoryItemTests
    {
        [Fact]
        public void InventoryItem_ShouldInstantiateWithAllProperties()
        {
            // Arrange & Act
            var item = new InventoryItem
            {
                ProductName = "Widget A",
                Quantity = 50,
                LastUpdated = DateTime.UtcNow
            };

            // Assert
            Assert.Equal("Widget A", item.ProductName);
            Assert.Equal(50, item.Quantity);
            Assert.NotEqual(default(DateTime), item.LastUpdated);
        }

        [Fact]
        public void InventoryItem_ShouldHaveProductNameProperty_GetAndSet()
        {
            // Arrange
            var item = new InventoryItem();

            // Act
            item.ProductName = "Product B";

            // Assert
            Assert.Equal("Product B", item.ProductName);
        }

        [Fact]
        public void InventoryItem_ShouldHaveQuantityProperty_GetAndSet()
        {
            // Arrange
            var item = new InventoryItem();

            // Act
            item.Quantity = 100;

            // Assert
            Assert.Equal(100, item.Quantity);
        }

        [Fact]
        public void InventoryItem_ShouldHaveLastUpdatedProperty_GetAndSet()
        {
            // Arrange
            var item = new InventoryItem();
            var testDate = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);

            // Act
            item.LastUpdated = testDate;

            // Assert
            Assert.Equal(testDate, item.LastUpdated);
        }

        [Fact]
        public void InventoryItem_ShouldHaveDefaultLastUpdatedValue()
        {
            // Arrange & Act
            var item = new InventoryItem();

            // Assert
            // Default should be close to DateTime.UtcNow (within a few seconds)
            var timeDifference = DateTime.UtcNow - item.LastUpdated;
            Assert.True(timeDifference.TotalSeconds < 5, "LastUpdated default should be close to DateTime.UtcNow");
        }

        [Fact]
        public void InventoryItem_ShouldAllowZeroQuantity()
        {
            // Arrange
            var item = new InventoryItem();

            // Act
            item.Quantity = 0;

            // Assert
            Assert.Equal(0, item.Quantity);
        }

        [Fact]
        public void InventoryItem_ShouldAllowNegativeQuantityInMemory()
        {
            // Arrange & Act
            var item = new InventoryItem
            {
                Quantity = -10
            };

            // Assert - In-memory, any int is allowed; validation happens at DataAnnotation level
            Assert.Equal(-10, item.Quantity);
        }

        [Fact]
        public void InventoryItem_ShouldStoreLastUpdatedAsUtc()
        {
            // Arrange
            var utcDate = new DateTime(2026, 5, 20, 15, 30, 45, DateTimeKind.Utc);

            // Act
            var item = new InventoryItem { LastUpdated = utcDate };

            // Assert
            Assert.Equal(DateTimeKind.Utc, item.LastUpdated.Kind);
            Assert.Equal(utcDate, item.LastUpdated);
        }
    }
}
