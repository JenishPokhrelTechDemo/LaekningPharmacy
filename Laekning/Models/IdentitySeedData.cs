using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Laekning.Models {

    // Static class to seed initial identity data (e.g., admin user) in the database
    public static class IdentitySeedData {

        // Ensure the database is populated with required identity data
        public static async Task EnsurePopulated(IApplicationBuilder app) {
            // Get the AppIdentityDbContext from the application's service provider
            var context = app.ApplicationServices
                .CreateScope().ServiceProvider
                .GetRequiredService<AppIdentityDbContext>();

            // Apply any pending EF Core migrations
            if (context.Database.GetPendingMigrations().Any()) {
                context.Database.Migrate();
            }

            // Key Vault configuration (Not recommended in production scenarios, this is shown for demonstration purposes)
            string keyVaultName = "laekningtestkeyvault"; //Add key vault name
            string vaultUri = $"https://{keyVaultName}.vault.azure.net/";

            // Create a client to access Azure Key Vault secrets
            var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());

            // Retrieve admin credentials from Key Vault
            KeyVaultSecret secretUsername = await client.GetSecretAsync("Username");
            KeyVaultSecret secretPassword = await client.GetSecretAsync("Password");

            string adminUser = secretUsername.Value;
            string adminPassword = secretPassword.Value;

            // Get the UserManager service for creating users
            var userManager = app.ApplicationServices
                .CreateScope().ServiceProvider
                .GetRequiredService<UserManager<IdentityUser>>();

            // Check if the admin user already exists
            IdentityUser? user = await userManager.FindByNameAsync(adminUser);

            if (user == null) {
                // If not, create a new admin user with specified credentials
                user = new IdentityUser(adminUser) {
                    Email = "admin@example.com",
                    PhoneNumber = "555-1234"
                };

                // Save the admin user with the password retrieved from Key Vault
                await userManager.CreateAsync(user, adminPassword);
            }
        }
    }
}
