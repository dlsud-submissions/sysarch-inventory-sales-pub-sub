extern alias AccountingServiceAPI;
extern alias InventoryServiceAPI;
extern alias SupplierServiceAPI;

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace InventorySalesApp.Tests.EndToEnd
{
    public class OrderFlowE2ETests : IAsyncLifetime
    {
        private const int ServiceBusDeliveryWaitMilliseconds = 3000;

        private WebApplicationFactory<global::Program> _mvcFactory = null!;
        private WebApplicationFactory<InventoryServiceAPI::Program> _inventoryFactory = null!;
        private WebApplicationFactory<AccountingServiceAPI::Program> _accountingFactory = null!;
        private WebApplicationFactory<SupplierServiceAPI::Program> _supplierFactory = null!;

        private HttpClient _mvcClient = null!;
        private HttpClient _inventoryClient = null!;
        private HttpClient _accountingClient = null!;
        private HttpClient _supplierClient = null!;

        [Fact]
        [Trait("Category", "E2E")]
        public async Task FullFlow_Prepaid_Pending_ShouldUpdate_InventoryAndAccounting()
        {
            var order = CreateOrder(orderType: "Prepaid", status: "Pending");

            var responseContent = await SubmitValidOrderAsync(order);
            await Task.Delay(ServiceBusDeliveryWaitMilliseconds);

            Assert.Contains("Order Submitted Successfully!", responseContent);
            Assert.Contains(order["OrderId"], responseContent);

            Assert.True(await InventoryContainsProductAsync(order["ProductName"]));
            Assert.True(await AccountingContainsOrderAsync(order["OrderId"]));
            Assert.False(await SupplierContainsProductAsync(order["ProductName"]));
        }

        [Fact]
        [Trait("Category", "E2E")]
        public async Task FullFlow_Prepaid_LowStock_ShouldUpdate_AllThreeSubscribers()
        {
            var order = CreateOrder(orderType: "Prepaid", status: "LowStock");

            var responseContent = await SubmitValidOrderAsync(order);
            await Task.Delay(ServiceBusDeliveryWaitMilliseconds);

            Assert.Contains("Order Submitted Successfully!", responseContent);
            Assert.Contains(order["OrderId"], responseContent);

            Assert.True(await InventoryContainsProductAsync(order["ProductName"]));
            Assert.True(await AccountingContainsOrderAsync(order["OrderId"]));
            Assert.True(await SupplierContainsProductAsync(order["ProductName"]));
        }

        [Fact]
        [Trait("Category", "E2E")]
        public async Task FullFlow_COD_Fulfilled_ShouldUpdate_InventoryOnly()
        {
            var order = CreateOrder(orderType: "COD", status: "Fulfilled");

            var responseContent = await SubmitValidOrderAsync(order);
            await Task.Delay(ServiceBusDeliveryWaitMilliseconds);

            Assert.Contains("Order Submitted Successfully!", responseContent);
            Assert.Contains(order["OrderId"], responseContent);

            Assert.True(await InventoryContainsProductAsync(order["ProductName"]));
            Assert.False(await AccountingContainsOrderAsync(order["OrderId"]));
            Assert.False(await SupplierContainsProductAsync(order["ProductName"]));
        }

        [Fact]
        [Trait("Category", "E2E")]
        public async Task FullFlow_InvalidModel_ShouldNotPublish_AndReturnForm()
        {
            var orderId = $"ORD-E2E-{Guid.NewGuid():N}";
            var productName = $"Invalid E2E Product {Guid.NewGuid():N}";

            var responseContent = await SubmitOrderFormAsync(new Dictionary<string, string>
            {
                ["OrderId"] = orderId,
                ["ProductName"] = productName,
                ["Quantity"] = "-1",
                ["OrderType"] = "InvalidType",
                ["Status"] = "InvalidStatus",
                ["Amount"] = "-100"
            });

            await Task.Delay(ServiceBusDeliveryWaitMilliseconds);

            Assert.Contains("Place a New Order", responseContent);
            Assert.DoesNotContain("Order Submitted Successfully!", responseContent);

            Assert.False(await InventoryContainsProductAsync(productName));
            Assert.False(await AccountingContainsOrderAsync(orderId));
            Assert.False(await SupplierContainsProductAsync(productName));
        }

        public async Task InitializeAsync()
        {
            var connectionString = GetServiceBusConnectionString();

            _mvcFactory = CreateFactory<global::Program>(connectionString);
            _inventoryFactory = CreateFactory<InventoryServiceAPI::Program>(connectionString);
            _accountingFactory = CreateFactory<AccountingServiceAPI::Program>(connectionString);
            _supplierFactory = CreateFactory<SupplierServiceAPI::Program>(connectionString);

            _mvcClient = _mvcFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true
            });

            _inventoryClient = _inventoryFactory.CreateClient();
            _accountingClient = _accountingFactory.CreateClient();
            _supplierClient = _supplierFactory.CreateClient();

            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _mvcClient?.Dispose();
            _inventoryClient?.Dispose();
            _accountingClient?.Dispose();
            _supplierClient?.Dispose();

            await _mvcFactory.DisposeAsync();
            await _inventoryFactory.DisposeAsync();
            await _accountingFactory.DisposeAsync();
            await _supplierFactory.DisposeAsync();
        }

        private static WebApplicationFactory<TProgram> CreateFactory<TProgram>(string connectionString)
            where TProgram : class
        {
            return new WebApplicationFactory<TProgram>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((_, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:ServiceBus"] = connectionString,
                            ["ServiceBus:ConnectionString"] = connectionString,
                            ["ServiceBus:TopicName"] = "orders"
                        });
                    });
                });
        }

        private static Dictionary<string, string> CreateOrder(string orderType, string status)
        {
            return new Dictionary<string, string>
            {
                ["OrderId"] = $"ORD-E2E-{Guid.NewGuid():N}",
                ["ProductName"] = $"E2E Product {Guid.NewGuid():N}",
                ["Quantity"] = "10",
                ["OrderType"] = orderType,
                ["Status"] = status,
                ["Amount"] = "150.50"
            };
        }

        private async Task<string> SubmitValidOrderAsync(Dictionary<string, string> order)
        {
            var responseContent = await SubmitOrderFormAsync(order);

            Assert.Contains("Order Submitted Successfully!", responseContent);
            Assert.Contains(order["OrderId"], responseContent);

            return responseContent;
        }

        private async Task<string> SubmitOrderFormAsync(Dictionary<string, string> formFields)
        {
            var getResponse = await _mvcClient.GetAsync("/SalesOrder/PlaceOrder");
            getResponse.EnsureSuccessStatusCode();

            var html = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractRequestVerificationToken(html);

            if (!string.IsNullOrWhiteSpace(token))
                formFields["__RequestVerificationToken"] = token;

            var postResponse = await _mvcClient.PostAsync(
                "/SalesOrder/PlaceOrder",
                new FormUrlEncodedContent(formFields));

            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            return await postResponse.Content.ReadAsStringAsync();
        }

        private async Task<bool> InventoryContainsProductAsync(string productName)
        {
            var json = await GetJsonAsync(_inventoryClient, "/api/inventory");
            return JsonArrayContainsProperty(json, "ProductName", "productName", productName);
        }

        private async Task<bool> AccountingContainsOrderAsync(string orderId)
        {
            var json = await GetJsonAsync(_accountingClient, "/api/accounting");
            return JsonArrayContainsProperty(json, "OrderId", "orderId", orderId);
        }

        private async Task<bool> SupplierContainsProductAsync(string productName)
        {
            var json = await GetJsonAsync(_supplierClient, "/api/supplier");
            return JsonArrayContainsProperty(json, "ProductName", "productName", productName);
        }

        private static async Task<string> GetJsonAsync(HttpClient client, string path)
        {
            var response = await client.GetAsync(path);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private static bool JsonArrayContainsProperty(
            string json,
            string pascalName,
            string camelName,
            string expectedValue)
        {
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty(pascalName, out var pascalValue)
                    && pascalValue.GetString() == expectedValue)
                {
                    return true;
                }

                if (item.TryGetProperty(camelName, out var camelValue)
                    && camelValue.GetString() == expectedValue)
                {
                    return true;
                }
            }

            return false;
        }

        private static string ExtractRequestVerificationToken(string html)
        {
            const string tokenName = "__RequestVerificationToken";
            var tokenNameIndex = html.IndexOf(tokenName, StringComparison.Ordinal);

            if (tokenNameIndex < 0)
                return string.Empty;

            var valueIndex = html.IndexOf("value=\"", tokenNameIndex, StringComparison.Ordinal);
            if (valueIndex < 0)
                return string.Empty;

            var valueStart = valueIndex + "value=\"".Length;
            var valueEnd = html.IndexOf('"', valueStart);

            return valueEnd > valueStart
                ? html[valueStart..valueEnd]
                : string.Empty;
        }

        private static string GetServiceBusConnectionString()
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<OrderFlowE2ETests>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("ServiceBus")
                ?? configuration["ConnectionStrings:ServiceBus"]
                ?? configuration["SERVICEBUS_CONNECTION_STRING"]
                ?? configuration["AZURE_SERVICEBUS_CONNECTION_STRING"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "A real Service Bus connection string is required. Set it in user secrets as " +
                    "'ConnectionStrings:ServiceBus' or in an environment variable named " +
                    "'SERVICEBUS_CONNECTION_STRING'.");
            }

            return connectionString;
        }
    }
}
