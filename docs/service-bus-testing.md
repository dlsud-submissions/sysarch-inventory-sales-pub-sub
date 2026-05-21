# Service Bus Testing

The live Integration and E2E tests use the real Azure Service Bus `orders` topic and these subscriptions:

- `inventory-subscription`: receives all orders through the `AllOrders` rule.
- `accounting-subscription`: receives only `OrderType = 'Prepaid'` through the `PrepaidOrdersOnly` rule.
- `supplier-subscription`: receives only `Status = 'LowStock'` through the `LowStockOnly` rule.

`ServiceBusSetup.ConfigureFiltersAsync()` is idempotent and repairs the expected rules before live tests publish messages. The tests use unique `OrderId` and `ProductName` values so repeated runs do not collide.

## Local Secrets

Store the connection string on the test project:

```bash
dotnet user-secrets init --project InventorySalesApp.Tests
dotnet user-secrets set "ConnectionStrings:ServiceBus" "<your-connection-string>" --project InventorySalesApp.Tests
```

The tests also accept `SERVICEBUS_CONNECTION_STRING` or `AZURE_SERVICEBUS_CONNECTION_STRING`.

## Commands

```bash
dotnet test InventorySalesApp.Tests/InventorySalesApp.Tests.csproj --filter "Category=Integration"
dotnet test InventorySalesApp.Tests/InventorySalesApp.Tests.csproj --filter "Category=E2E"
dotnet test InventorySalesApp.Tests/InventorySalesApp.Tests.csproj --filter "Category!=Integration&Category!=E2E"
```

If a live test fails, check the Azure Portal subscription rules and message counts first. The inventory subscription must have either `$Default` or `AllOrders`; the test setup creates `AllOrders` when it is missing.
