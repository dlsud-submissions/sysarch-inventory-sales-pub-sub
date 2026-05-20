using InventoryService.API.Models;
using InventoryService.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly InventorySubscriber? _subscriber;

        public InventoryController(InventorySubscriber? subscriber)
        {
            _subscriber = subscriber;
        }

        /// <summary>
        /// GET /api/inventory — returns current in-memory stock list
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<InventoryItem>> GetInventory()
        {
            var items = _subscriber?.GetInventory() ?? Enumerable.Empty<InventoryItem>();
            return Ok(items);
        }
    }
}