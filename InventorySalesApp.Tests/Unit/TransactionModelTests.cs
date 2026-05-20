extern alias AccountingServiceAPI;

using AccountingServiceAPI::AccountingService.API.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace InventorySalesApp.Tests.Unit
{
    public class TransactionModelTests
    {
        [Fact]
        public void Transaction_ShouldInstantiateWithAllProperties()
        {
            var transaction = new Transaction
            {
                TransactionId = "TXN-001",
                OrderId = "ORD-001",
                Amount = 4500.00m,
                OrderType = "Prepaid",
                Timestamp = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc)
            };

            Assert.Equal("TXN-001", transaction.TransactionId);
            Assert.Equal("ORD-001", transaction.OrderId);
            Assert.Equal(4500.00m, transaction.Amount);
            Assert.Equal("Prepaid", transaction.OrderType);
            Assert.Equal(new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc), transaction.Timestamp);
        }

        [Fact]
        public void Transaction_ShouldHaveTransactionIdProperty_GetAndSet()
        {
            var transaction = new Transaction();
            transaction.TransactionId = "TXN-002";
            Assert.Equal("TXN-002", transaction.TransactionId);
        }

        [Fact]
        public void Transaction_ShouldHaveOrderIdProperty_GetAndSet()
        {
            var transaction = new Transaction();
            transaction.OrderId = "ORD-002";
            Assert.Equal("ORD-002", transaction.OrderId);
        }

        [Fact]
        public void Transaction_ShouldHaveAmountProperty_GetAndSet()
        {
            var transaction = new Transaction();
            transaction.Amount = 1500.75m;
            Assert.Equal(1500.75m, transaction.Amount);
        }

        [Fact]
        public void Transaction_ShouldHaveOrderTypeProperty_GetAndSet()
        {
            var transaction = new Transaction();
            transaction.OrderType = "COD";
            Assert.Equal("COD", transaction.OrderType);
        }

        [Fact]
        public void Transaction_ShouldHaveTimestampProperty_GetAndSet()
        {
            var transaction = new Transaction();
            var testDate = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc);
            transaction.Timestamp = testDate;
            Assert.Equal(testDate, transaction.Timestamp);
        }

        [Fact]
        public void Transaction_ShouldHaveDefaultTimestamp_CloseToUtcNow()
        {
            var transaction = new Transaction();
            var diff = DateTime.UtcNow - transaction.Timestamp;
            Assert.True(diff.TotalSeconds < 5, "Timestamp default should be close to DateTime.UtcNow");
        }

        [Fact]
        public void Transaction_ShouldHaveKeyAttribute_OnTransactionId()
        {
            var property = typeof(Transaction).GetProperty(nameof(Transaction.TransactionId));
            Assert.NotNull(property);
            Assert.True(property.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0,
                "TransactionId should have [Key] attribute");
        }

        [Fact]
        public void Transaction_ShouldAcceptPrepaidOrderType()
        {
            var transaction = new Transaction { TransactionId = "TXN-003", OrderId = "ORD-003", Amount = 100m, OrderType = "Prepaid" };
            var context = new ValidationContext(transaction);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(transaction, context, results, true);
            Assert.True(isValid, "Prepaid OrderType should be valid");
        }

        [Fact]
        public void Transaction_ShouldAcceptCODOrderType()
        {
            var transaction = new Transaction { TransactionId = "TXN-004", OrderId = "ORD-004", Amount = 200m, OrderType = "COD" };
            var context = new ValidationContext(transaction);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(transaction, context, results, true);
            Assert.True(isValid, "COD OrderType should be valid");
        }

        [Fact]
        public void Transaction_ShouldAllowZeroAmount()
        {
            var transaction = new Transaction { TransactionId = "TXN-005", OrderId = "ORD-005", Amount = 0m, OrderType = "Prepaid" };
            var context = new ValidationContext(transaction);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(transaction, context, results, true);
            Assert.True(isValid, "Amount of 0 should be valid");
        }

        [Fact]
        public void Transaction_ShouldAllowModificationOfAllProperties()
        {
            var transaction = new Transaction
            {
                TransactionId = "TXN-001",
                OrderId = "ORD-001",
                Amount = 100m,
                OrderType = "Prepaid",
                Timestamp = DateTime.UtcNow
            };

            transaction.TransactionId = "TXN-999";
            transaction.OrderId = "ORD-999";
            transaction.Amount = 9999.99m;
            transaction.OrderType = "COD";
            transaction.Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            Assert.Equal("TXN-999", transaction.TransactionId);
            Assert.Equal("ORD-999", transaction.OrderId);
            Assert.Equal(9999.99m, transaction.Amount);
            Assert.Equal("COD", transaction.OrderType);
            Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), transaction.Timestamp);
        }
    }
}