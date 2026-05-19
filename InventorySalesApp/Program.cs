var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register SalesOrderPublisher as singleton so it can be injected
builder.Services.AddSingleton<InventorySalesApp.Services.SalesOrderPublisher>();
// Register ServiceBusSetup to configure filters on startup
builder.Services.AddSingleton<InventorySalesApp.Services.ServiceBusSetup>();

var app = builder.Build();

// Configure Service Bus filters at startup (non-blocking, with error handling)
_ = Task.Run(async () =>
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var setup = scope.ServiceProvider.GetRequiredService<InventorySalesApp.Services.ServiceBusSetup>();
            await setup.ConfigureFiltersAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to configure Service Bus filters at startup");
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
