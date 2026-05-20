using Microsoft.AspNetCore.Mvc;
using SupplierService.API.Models;
using SupplierService.API.Services;

namespace SupplierService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupplierController : ControllerBase
    {
        private readonly SupplierSubscriber? _subscriber;

        public SupplierController(SupplierSubscriber? subscriber)
        {
            _subscriber = subscriber;
        }

        /// <summary>
        /// GET /api/supplier - returns current in-memory reorder alerts.
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<ReorderAlert>> GetSupplier()
        {
            var alerts = _subscriber?.GetReorderAlerts() ?? Enumerable.Empty<ReorderAlert>();
            return Ok(alerts);
        }
    }
}
