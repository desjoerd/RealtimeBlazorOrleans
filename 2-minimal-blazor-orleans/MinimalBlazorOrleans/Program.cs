using MinimalBlazorOrleans.Components;
using MinimalBlazorOrleans.Grains.Filters;

var builder = WebApplication.CreateBuilder(args);
// Aspire service defaults
builder.AddServiceDefaults();
//
// builder.AddKeyedAzureBlobClient("blobs");
// builder.AddKeyedAzureTableClient("tables");

builder.UseOrleans(options =>
{
#pragma warning disable ORLEANSEXP003
    options.AddDistributedGrainDirectory();
#pragma warning restore ORLEANSEXP003

    options.UseLocalhostClustering();
    options.UseDashboard(x => x.HostSelf = true);

    options.AddIncomingGrainCallFilter(new BlazorIncomingGrainFilter());

    options.AddActivityPropagation();
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("Microsoft.Orleans.Application"));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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