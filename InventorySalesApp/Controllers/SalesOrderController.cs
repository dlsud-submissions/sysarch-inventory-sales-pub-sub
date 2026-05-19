using InventorySalesApp.Models;
using InventorySalesApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InventorySalesApp.Controllers
{
    public class SalesOrderController : Controller
    {
        private readonly ISalesOrderPublisher _publisher;

        public SalesOrderController(ISalesOrderPublisher publisher)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        // GET: /SalesOrder/PlaceOrder
        public IActionResult PlaceOrder()
        {
            return View(new SalesOrderModel());
        }

        // POST: /SalesOrder/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(SalesOrderModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Publish order to Service Bus
                await _publisher.PublishOrderAsync(model);

                // Pass order to confirmation view
                return RedirectToAction(nameof(OrderConfirmation), model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error publishing order: {ex.Message}");
                return View(model);
            }
        }

        // GET: /SalesOrder/OrderConfirmation
        public IActionResult OrderConfirmation(SalesOrderModel model)
        {
            if (model == null)
            {
                return RedirectToAction(nameof(PlaceOrder));
            }

            return View(model);
        }
    }
}
