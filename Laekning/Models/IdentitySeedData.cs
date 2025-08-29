using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Laekning.Models {

    public static class IdentitySeedData {
        public static async Task EnsurePopulated(IApplicationBuilder app) {
            var context = app.ApplicationServices
                .CreateScope().ServiceProvider
                .GetRequiredService<AppIdentityDbContext>();

            if (context.Database.GetPendingMigrations().Any()) {
                context.Database.Migrate();
            }
			
			string keyVaultName = "laekningtestkeyvault";
            string vaultUri = $"https://{keyVaultName}.vault.azure.net/";
            var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());

            KeyVaultSecret secretUsername = await client.GetSecretAsync("Username");
            KeyVaultSecret secretPassword = await client.GetSecretAsync("Password");

            string adminUser = secretUsername.Value;
            string adminPassword = secretPassword.Value;

            var userManager = app.ApplicationServices
                .CreateScope().ServiceProvider
                .GetRequiredService<UserManager<IdentityUser>>();

            IdentityUser? user = await userManager.FindByNameAsync(adminUser);
            if (user == null) {
                user = new IdentityUser(adminUser) {
                    Email = "admin@example.com",
                    PhoneNumber = "555-1234"
                };
                await userManager.CreateAsync(user, adminPassword);
            }
        }
    }
}
