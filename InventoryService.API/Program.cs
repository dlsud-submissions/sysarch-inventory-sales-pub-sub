var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register InventorySubscriber as a hosted background service
builder.Services.AddSingleton<InventoryService.API.Services.InventorySubscriber>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<InventoryService.API.Services.InventorySubscriber>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
