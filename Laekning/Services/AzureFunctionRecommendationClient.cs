using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Laekning.Models;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Laekning.Services
{
    // Service client for calling an Azure Function to get product recommendations
    public class AzureFunctionRecommendationClient
    {
        private readonly HttpClient _httpClient; // HTTP client for making requests
        private readonly IConfiguration _config; // Configuration to read settings (e.g., Key Vault URL)

        // Constructor to inject HttpClient and configuration
        public AzureFunctionRecommendationClient(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        // Calls the Azure Function to get recommended products based on purchased products
        public async Task<List<string>> GetRecommendedProductsAsync(List<string> purchasedProductDescriptions, List<string> allDbProductNames)
        {
            // Get Key Vault URI from configuration
            string vaultUri = _config["AzureKeyVault:KeyVaultUrl"];
            
            // Create a Key Vault client using managed identity or default credential
            var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());

            // Retrieve the Azure Function URL stored in Key Vault
            KeyVaultSecret secretAzureFunctionsRecommendrl = client.GetSecret("AzureFunctionsRecommendUrl");
            var functionUrl = secretAzureFunctionsRecommendrl.Value;  // URL of the recommendation function
            
            var functionKey = "";  // Optional function key for authentication if needed

            // Prepare payload to send to the Azure Function
            var payload = new
            {
                purchasedProductDescriptions,    // List of description of products the user already purchased
                allDbProductNames     // List of all products in the database
            };

            // Create an HTTP POST request with JSON payload
            var request = new HttpRequestMessage(HttpMethod.Post, functionUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            // Include the function key in the request headers if required
            if (!string.IsNullOrEmpty(functionKey))
            {
                request.Headers.Add("x-functions-key", functionKey);
            }

            // Send the request to the Azure Function
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode(); // Throws exception if status code is not success

            // Read the JSON response
            var json = await response.Content.ReadAsStringAsync();

            // Parse the JSON response
            var doc = JsonDocument.Parse(json);

            // Extract the "recommendedProducts" array from the response
            if (doc.RootElement.TryGetProperty("recommendedProducts", out var recs))
            {
                // Convert JSON array to a List<string>
                return recs.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
            }

            // Return empty list if no recommendations found
            return new List<string>();
        }
    }
}
