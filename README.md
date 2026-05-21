# InventorySalesSystem

This repository contains the Inventory Sales System solution used in the course.

Architecture diagram (Batch 1):

![Architecture](docs/architecture-diagram.png)

## Projects

- InventorySalesApp (Razor Pages / MVC front-end)
- InventoryService.API (inventory microservice)
- AccountingService.API (accounting microservice)
- SupplierService.API (supplier microservice)
- InventorySalesApp.Tests (xUnit tests)

## Deployment

### Live Deployment

The MVC Publisher is deployed to Azure App Service:

**URL:** https://inventory-sales-app.azurewebsites.net

**Status:** ![Deploy MVC Publisher to Azure App Service](https://github.com/dlsud-submissions/sysarch-inventory-sales-pub-sub/actions/workflows/deploy-mvc.yml/badge.svg)

### Deployment Pipeline

- **Trigger:** Push to `main` branch or manual workflow dispatch
- **Build:** .NET 10 on Ubuntu
- **Tests:** Unit tests only (no Integration/E2E in CI)
- **Publish:** Release configuration to Azure App Service
- **Environment Variables:** `CONNECTIONSTRINGS__SERVICEBUS` (set in Azure Portal)

## Local Setup

See [docs/service-bus-testing.md](docs/service-bus-testing.md) for the live Service Bus rule setup and test verification workflow.

### Prerequisites

- .NET 10 SDK
- Azure CLI (optional, for local testing)
- User Secrets configured with Service Bus connection string

### Build and Test

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run all tests
dotnet test

# Run unit tests only (no Azure connection required)
dotnet test --filter "Category!=Integration&Category!=E2E"

# Run live integration tests (requires Azure Service Bus)
dotnet test InventorySalesApp.Tests/InventorySalesApp.Tests.csproj --filter "Category=Integration"

# Run live end-to-end tests (requires Azure Service Bus)
dotnet test InventorySalesApp.Tests/InventorySalesApp.Tests.csproj --filter "Category=E2E"
```

### User Secrets Setup

Store your Azure Service Bus connection string locally:

```bash
dotnet user-secrets init --project InventorySalesApp.Tests

dotnet user-secrets set "ConnectionStrings:ServiceBus" "<your-connection-string>" --project InventorySalesApp.Tests
```

**Important:** Ensure you store secrets using user secrets or environment variables. `appsettings.json` files should not be committed. The `.gitignore` file excludes all secrets files from version control.
