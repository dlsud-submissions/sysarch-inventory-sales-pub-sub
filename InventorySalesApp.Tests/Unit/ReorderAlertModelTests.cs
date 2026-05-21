extern alias SupplierServiceAPI;

using SupplierServiceAPI::SupplierService.API.Models;
using System;
using Xunit;

namespace InventorySalesApp.Tests.Unit
{
    public class ReorderAlertModelTests
    {
        [Fact]
        public void ReorderAlert_ShouldInstantiateWithAllProperties()
        {
            var alert = new ReorderAlert
            {
                ProductName = "Steel Bolts",
                QuantityNeeded = 100,
                AlertTimestamp = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc)
            };

            Assert.Equal("Steel Bolts", alert.ProductName);
            Assert.Equal(100, alert.QuantityNeeded);
            Assert.Equal(new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc), alert.AlertTimestamp);
        }

        [Fact]
        public void ReorderAlert_ShouldHaveProductNameProperty_GetAndSet()
        {
            var alert = new ReorderAlert();

            alert.ProductName = "Machine Screws";

            Assert.Equal("Machine Screws", alert.ProductName);
        }

        [Fact]
        public void ReorderAlert_ShouldHaveQuantityNeededProperty_GetAndSet()
        {
            var alert = new ReorderAlert();

            alert.QuantityNeeded = 250;

            Assert.Equal(250, alert.QuantityNeeded);
        }

        [Fact]
        public void ReorderAlert_ShouldHaveAlertTimestampProperty_GetAndSet()
        {
            var alert = new ReorderAlert();
            var timestamp = new DateTime(2026, 5, 20, 14, 30, 0, DateTimeKind.Utc);

            alert.AlertTimestamp = timestamp;

            Assert.Equal(timestamp, alert.AlertTimestamp);
        }

        [Fact]
        public void ReorderAlert_ShouldHaveDefaultProductName()
        {
            var alert = new ReorderAlert();

            Assert.Equal(string.Empty, alert.ProductName);
        }

        [Fact]
        public void ReorderAlert_ShouldHaveDefaultAlertTimestamp_CloseToUtcNow()
        {
            var alert = new ReorderAlert();

            var diff = DateTime.UtcNow - alert.AlertTimestamp;

            Assert.True(diff.TotalSeconds < 5, "AlertTimestamp default should be close to DateTime.UtcNow");
        }

        [Fact]
        public void ReorderAlert_ShouldAllowZeroQuantityNeeded()
        {
            var alert = new ReorderAlert { QuantityNeeded = 0 };

            Assert.Equal(0, alert.QuantityNeeded);
        }
    }
}
