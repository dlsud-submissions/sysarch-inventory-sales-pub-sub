namespace SupplierService.API.Models
{
    public class ReorderAlert
    {
        public string ProductName { get; set; } = string.Empty;

        public int QuantityNeeded { get; set; }

        public DateTime AlertTimestamp { get; set; } = DateTime.UtcNow;
    }
}
