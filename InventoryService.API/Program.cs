using InventoryService.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register InventorySubscriber as a hosted background service AND as a singleton
// so InventoryController can inject it and call GetInventory()
builder.Services.AddSingleton<InventorySubscriber>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<InventorySubscriber>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }