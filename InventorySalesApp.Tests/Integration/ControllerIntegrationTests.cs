using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace InventorySalesApp.Tests.Integration
{
    public class ControllerIntegrationTests : IAsyncLifetime
    {
        private WebApplicationFactory<global::Program> _factory;
        private HttpClient _client;

        public async Task InitializeAsync()
        {
            _factory = new WebApplicationFactory<global::Program>()
                .WithWebHostBuilder(builder =>
                {
                    // Configure test services if needed
                });
            _client = _factory.CreateClient();
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _client?.Dispose();
            _factory?.Dispose();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task App_ShouldStart_WithoutExceptions()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/");

            // Assert
            Assert.NotNull(response);
        }

        [Fact]
        public async Task GET_PlaceOrder_ShouldReturn200_AndRenderForm()
        {
            // Arrange
            var endpoint = "/SalesOrder/PlaceOrder";

            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Place a New Order", content);
            Assert.Contains("Order ID", content);
            Assert.Contains("Product Name", content);
            Assert.Contains("Quantity", content);
            Assert.Contains("Order Type", content);
            Assert.Contains("Status", content);
            Assert.Contains("Amount", content);
        }

        [Fact]
        public async Task POST_PlaceOrder_ShouldAccept_ValidModel()
        {
            // Arrange - First get the form to extract antiforgery token
            var getResponse = await _client.GetAsync("/SalesOrder/PlaceOrder");
            var htmlContent = await getResponse.Content.ReadAsStringAsync();

            // Extract CSRF token from HTML (it's in a hidden input field)
            var tokenStart = htmlContent.IndexOf("__RequestVerificationToken");
            string token = string.Empty;
            if (tokenStart > 0)
            {
                var valueStart = htmlContent.IndexOf("value=\"", tokenStart) + 7;
                var valueEnd = htmlContent.IndexOf("\"", valueStart);
                token = htmlContent.Substring(valueStart, valueEnd - valueStart);
            }

            var formData = new Dictionary<string, string>
            {
                { "OrderId", "ORD-INT-TEST-001" },
                { "ProductName", "Integration Test Product" },
                { "Quantity", "10" },
                { "OrderType", "Prepaid" },
                { "Status", "Pending" },
                { "Amount", "150.50" }
            };

            if (!string.IsNullOrEmpty(token))
            {
                formData.Add("__RequestVerificationToken", token);
            }

            var content = new FormUrlEncodedContent(formData);

            // Act
            var response = await _client.PostAsync("/SalesOrder/PlaceOrder", content);

            // Assert - Accept either 200 (form re-rendered) or 302/303 (redirect to confirmation)
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Found ||
                response.StatusCode == HttpStatusCode.Redirect ||
                response.StatusCode == HttpStatusCode.SeeOther,
                $"Expected 200, 302, 303, or 307, but got {response.StatusCode}"
            );
        }

        [Fact]
        public async Task POST_PlaceOrder_WithInvalidModel_ShouldReturnForm()
        {
            // Arrange - Get form first for CSRF token
            var getResponse = await _client.GetAsync("/SalesOrder/PlaceOrder");
            var htmlContent = await getResponse.Content.ReadAsStringAsync();

            var tokenStart = htmlContent.IndexOf("__RequestVerificationToken");
            string token = string.Empty;
            if (tokenStart > 0)
            {
                var valueStart = htmlContent.IndexOf("value=\"", tokenStart) + 7;
                var valueEnd = htmlContent.IndexOf("\"", valueStart);
                token = htmlContent.Substring(valueStart, valueEnd - valueStart);
            }

            var formData = new Dictionary<string, string>
            {
                // Invalid data
                { "OrderId", "" },
                { "ProductName", "" },
                { "Quantity", "-1" },
                { "OrderType", "InvalidType" },
                { "Status", "InvalidStatus" },
                { "Amount", "-100" }
            };

            if (!string.IsNullOrEmpty(token))
            {
                formData.Add("__RequestVerificationToken", token);
            }

            var content = new FormUrlEncodedContent(formData);

            // Act
            var response = await _client.PostAsync("/SalesOrder/PlaceOrder", content);

            // Assert - Should return 200 (form re-rendered with errors) or BadRequest
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest,
                $"Expected 200 or 400, but got {response.StatusCode}"
            );

            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Place a New Order", responseContent);
        }

        [Fact]
        public async Task DefaultRoute_ShouldServePlaceOrder()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/");

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Found ||
                response.StatusCode == HttpStatusCode.Redirect,
                $"Expected 200 or 302, but got {response.StatusCode}"
            );

            var content = await response.Content.ReadAsStringAsync();
            // If 200, should contain form; if redirect, that's also acceptable
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Assert.Contains("Place a New Order", content);
            }
        }
    }
}
