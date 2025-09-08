using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Laekning.Models;
using Laekning.Services;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

// Add MVC Controllers with Views support
builder.Services.AddControllersWithViews();

// -------------------------
// Retrieve secrets from Azure Key Vault
// -------------------------
string vaultUri = builder.Configuration["AzureKeyVault:KeyVaultUrl"];
var secretClient = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());

// Get SQL connection string for the main Store database
KeyVaultSecret secretLaekningConnection = await secretClient.GetSecretAsync("LaekningConnection");
string sqlLaekningConnectionString = secretLaekningConnection.Value;

// Get SQL connection string for Identity database
KeyVaultSecret secretIdentityConnection = await secretClient.GetSecretAsync("IdentityConnection");
string sqlIdentityConnectionString = secretIdentityConnection.Value;

// -------------------------
// Configure main StoreDbContext with SQL Server and retry policy
// -------------------------
builder.Services.AddDbContext<StoreDbContext>(opts =>
{
    opts.UseSqlServer(
        sqlLaekningConnectionString,
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,              // Retry transient failures up to 5 times
            maxRetryDelay: TimeSpan.FromSeconds(10), // Wait max 10s between retries
            errorNumbersToAdd: null        // Default SQL transient errors
        ));
});

// Register repository services for dependency injection
builder.Services.AddScoped<IStoreRepository, EFStoreRepository>();
builder.Services.AddScoped<IOrderRepository, EFOrderRepository>();

// Register helper services
builder.Services.AddScoped<OcrGptSearchHelper>();

// Razor Pages support
builder.Services.AddRazorPages();

// Session state support (in-memory)
builder.Services.AddDistributedMemoryCache();

// Event Hub client singleton
builder.Services.AddSingleton<EventHubSender>();

// Configure session options
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session expires after 30 mins inactivity
    options.Cookie.HttpOnly = true;                // Prevent client-side access to cookies
    options.Cookie.IsEssential = true;             // Required for GDPR/non-EU consent rules
});

// Register Cart using session-based factory
builder.Services.AddScoped<Cart>(sp => SessionCart.GetCart(sp));
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Blazor Server support
builder.Services.AddServerSideBlazor();

// -------------------------
// Configure Identity DB Context and ASP.NET Identity
// -------------------------
builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseSqlServer(
        sqlIdentityConnectionString,
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppIdentityDbContext>();

// Register GPT recommender and Azure Function client
builder.Services.AddScoped<GptProductRecommender>();
builder.Services.AddHttpClient<AzureFunctionRecommendationClient>();

// -------------------------
// Build the WebApplication
// -------------------------
var app = builder.Build();

// Use exception handler in production environment
if (app.Environment.IsProduction())
{
    app.UseExceptionHandler("/error");
}

// Configure localization (en-US)
app.UseRequestLocalization(opts =>
{
    opts.AddSupportedCultures("en-US")
        .AddSupportedUICultures("en-US")
        .SetDefaultCulture("en-US");
});

// Serve static files (wwwroot)
app.UseStaticFiles();

// Routing must come before session/auth
app.UseRouting();

// Enable session and authentication/authorization
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// -------------------------
// Configure route patterns
// -------------------------

// Category + Pagination route
app.MapControllerRoute("catpage",
    "{category}/Page{productPage:int}",
    new { Controller = "Home", action = "Index" });

// Pagination only
app.MapControllerRoute("page", "Page{productPage:int}",
    new { Controller = "Home", action = "Index", productPage = 1 });

// Category only
app.MapControllerRoute("category", "{category}",
    new { Controller = "Home", action = "Index", productPage = 1 });

// Generic pagination route
app.MapControllerRoute("pagination",
    "Products/Page{productPage}",
    new { Controller = "Home", action = "Index", productPage = 1 });

// Default MVC route
app.MapDefaultControllerRoute();

// Razor Pages routing
app.MapRazorPages();

// Blazor Server Hub
app.MapBlazorHub();

// Fallback route for admin pages
app.MapFallbackToPage("/admin/{*catchall}", "/Admin/Index");

// -------------------------
// Seed databases if empty
// -------------------------
SeedData.EnsurePopulated(app);
IdentitySeedData.EnsurePopulated(app);

// Run the application
app.Run();
