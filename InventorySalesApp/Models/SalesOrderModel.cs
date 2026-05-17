using System.ComponentModel.DataAnnotations;

namespace InventorySalesApp.Models
{
    public class SalesOrderModel
    {
        [Key]
        [Required(ErrorMessage = "Order ID is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Order ID must be between 1 and 50 characters")]
        public string OrderId { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Product name must be between 1 and 100 characters")]
        public string ProductName { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Order type is required")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Order type must be between 1 and 20 characters")]
        [RegularExpression(@"^(Prepaid|COD)$", ErrorMessage = "Order type must be either 'Prepaid' or 'COD'")]
        public string OrderType { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Status must be between 1 and 50 characters")]
        [RegularExpression(@"^(Pending|Fulfilled|LowStock)$", ErrorMessage = "Status must be either 'Pending', 'Fulfilled', or 'LowStock'")]
        public string Status { get; set; }

        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Amount must be greater than or equal to 0")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }
    }
}
