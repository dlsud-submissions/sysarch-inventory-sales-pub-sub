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

## Setup

1. dotnet build
2. dotnet test

Ensure you store secrets using user secrets or environment variables. appsettings.json files should not be committed.
