using AccountingService.API.Models;
using AccountingService.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccountingService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountingController : ControllerBase
    {
        private readonly AccountingSubscriber? _subscriber;

        public AccountingController(AccountingSubscriber? subscriber)
        {
            _subscriber = subscriber;
        }

        /// <summary>
        /// GET /api/accounting - returns current in-memory transaction records.
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<Transaction>> GetAccounting()
        {
            var transactions = _subscriber?.GetTransactions() ?? Enumerable.Empty<Transaction>();
            return Ok(transactions);
        }
    }
}
