using System.ComponentModel.DataAnnotations;

namespace AccountingService.API.Models
{
    public class Transaction
    {
        [Key]
        [Required(ErrorMessage = "Transaction ID is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Transaction ID must be between 1 and 50 characters")]
        public string TransactionId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Order ID is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Order ID must be between 1 and 50 characters")]
        public string OrderId { get; set; } = string.Empty;

        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Amount must be greater than or equal to 0")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Order type is required")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Order type must be between 1 and 20 characters")]
        [RegularExpression(@"^(Prepaid|COD)$", ErrorMessage = "Order type must be either 'Prepaid' or 'COD'")]
        public string OrderType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Timestamp is required")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
