using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Laekning.Models;
using Laekning.Services;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

string vaultUri = builder.Configuration["AzureKeyVault:KeyVaultUrl"];
var secretClient = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());

KeyVaultSecret secretLaekningConnection = await secretClient.GetSecretAsync("LaekningConnection");
string sqlLaekningConnectionString = secretLaekningConnection.Value;

KeyVaultSecret secretIdentityConnection = await secretClient.GetSecretAsync("IdentityConnection");
string sqlIdentityConnectionString = secretIdentityConnection.Value;

builder.Services.AddDbContext<StoreDbContext>(opts =>
{
    opts.UseSqlServer(
        sqlLaekningConnectionString,
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,              // retry up to 5 times
            maxRetryDelay: TimeSpan.FromSeconds(10), // wait up to 10s between retries
            errorNumbersToAdd: null        // use default transient errors
        ));
});

builder.Services.AddScoped<IStoreRepository, EFStoreRepository>();
builder.Services.AddScoped<IOrderRepository, EFOrderRepository>();


builder.Services.AddScoped<OcrGptSearchHelper>();



builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();

//builder.Services.AddSingleton<EventHubSender>();


// Configure Session options
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Keep chat alive for 30 minutes of inactivity
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Required for non-EU cookie consent rules
});

builder.Services.AddScoped<Cart>(sp => SessionCart.GetCart(sp));
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddServerSideBlazor();

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

builder.Services.AddScoped<GptProductRecommender>();
builder.Services.AddHttpClient<AzureFunctionRecommendationClient>();

var app = builder.Build();

if (app.Environment.IsProduction())
{
    app.UseExceptionHandler("/error");
}

app.UseRequestLocalization(opts =>
{
    opts.AddSupportedCultures("en-US")
        .AddSupportedUICultures("en-US")
        .SetDefaultCulture("en-US");
});

app.UseStaticFiles();

// Routing must come before Session/Auth
app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("catpage",
    "{category}/Page{productPage:int}",
    new { Controller = "Home", action = "Index" });

app.MapControllerRoute("page", "Page{productPage:int}",
    new { Controller = "Home", action = "Index", productPage = 1 });

app.MapControllerRoute("category", "{category}",
    new { Controller = "Home", action = "Index", productPage = 1 });

app.MapControllerRoute("pagination",
    "Products/Page{productPage}",
    new { Controller = "Home", action = "Index", productPage = 1 });

app.MapDefaultControllerRoute();
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/admin/{*catchall}", "/Admin/Index");

SeedData.EnsurePopulated(app);
IdentitySeedData.EnsurePopulated(app);

app.Run();
