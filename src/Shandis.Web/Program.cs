using Shandis.Web.Components;
using Shandis.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Razor components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Shandis API
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? "http://localhost:5288/";

builder.Services.AddHttpClient<ShandisApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
});

builder.Services.AddScoped<ShandisAuthService>();

var app = builder.Build();

// HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();