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
        public async Task<List<string>> GetRecommendedProductsAsync	(List<Product> purchasedProducts, List<Product> allDbProducts)
		{
			// Key Vault retrieval code remains the same
			string vaultUri = _config["AzureKeyVault:KeyVaultUrl"];
			var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
			var functionUrl = client.GetSecret("AzureFunctionsRecommendUrl").Value;
    
			// Build payloads with name + category
			var purchasedPayload = purchasedProducts
				.Select(p => new { name = p.Name, category = p.Category })
				.ToList();

			var allProductsPayload = allDbProducts
				.Select(p => new { name = p.Name, category = p.Category })
				.ToList();

			var payload = new
			{
				purchasedProducts = purchasedPayload,
				allProducts = allProductsPayload
			};

			// Send POST request
			var request = new HttpRequestMessage(HttpMethod.Post, functionUrl)
			{
				Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
			};

			var response = await _httpClient.SendAsync(request);
			response.EnsureSuccessStatusCode();

			var json = await response.Content.ReadAsStringAsync();
			var doc = JsonDocument.Parse(json);

			if (doc.RootElement.TryGetProperty("recommendedProducts", out var recs))
			{
				return recs.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
			}

			return new List<string>();
		}

    }
}
