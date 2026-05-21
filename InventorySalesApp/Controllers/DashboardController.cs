using InventorySalesApp.Models;
using InventorySalesApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventorySalesApp.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ISubscriptionStatsService _subscriptionStatsService;

        public DashboardController(ISubscriptionStatsService subscriptionStatsService)
        {
            _subscriptionStatsService = subscriptionStatsService;
        }

        [HttpGet("/Dashboard")]
        [HttpGet("/Dashboard/Index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var stats = await _subscriptionStatsService.GetAllSubscriptionStatsAsync().ConfigureAwait(false);
                return View(stats);
            }
            catch
            {
                ViewData["DashboardError"] = "Subscription stats are temporarily unavailable.";
                return View(new List<SubscriptionStatsViewModel>());
            }
        }
    }
}
