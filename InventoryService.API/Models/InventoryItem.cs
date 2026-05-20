using System.ComponentModel.DataAnnotations;

namespace InventoryService.API.Models
{
    public class InventoryItem
    {
        [Key]
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Product name must be between 1 and 100 characters")]
        public string ProductName { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be greater than or equal to 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Last updated timestamp is required")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
