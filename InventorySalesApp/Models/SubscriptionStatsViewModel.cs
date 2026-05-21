namespace InventorySalesApp.Models
{
    public class SubscriptionStatsViewModel
    {
        public string SubscriptionName { get; set; } = string.Empty;

        public long ActiveMessageCount { get; set; }

        public long DeadLetterMessageCount { get; set; }

        public long TransferMessageCount { get; set; }

        public long TotalMessageCount { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime LastRefreshed { get; set; }
    }
}
