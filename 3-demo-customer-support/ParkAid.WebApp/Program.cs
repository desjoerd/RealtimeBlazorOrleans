using ParkAid.WebApp.Blazor;
using ParkAid.WebApp.Components;

var builder = WebApplication.CreateBuilder(args);
// Aspire service defaults
builder.AddServiceDefaults();

builder.AddKeyedAzureBlobClient("blobs");
builder.AddKeyedAzureTableClient("tables", settings => settings.DisableHealthChecks = true);

builder.UseOrleans(options =>
{
    options.AddDistributedGrainDirectory();

    options.UseDashboard(x =>
    {
        x.HostSelf = false;
    });

    options.AddIncomingGrainCallFilter(new BlazorIncomingGrainFilter());

    options.AddActivityPropagation();
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapDefaultEndpoints();
app.Map("/dashboard", x => x.UseOrleansDashboard());

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();