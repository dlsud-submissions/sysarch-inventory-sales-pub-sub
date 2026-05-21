using InventorySalesApp.Models;
using System.Threading.Tasks;

namespace InventorySalesApp.Services
{
    public interface ISalesOrderPublisher
    {
        Task PublishOrderAsync(SalesOrderModel order);
    }
}
