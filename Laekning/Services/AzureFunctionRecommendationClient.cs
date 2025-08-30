using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Laekning.Models;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Laekning.Services
{
    public class AzureFunctionRecommendationClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public AzureFunctionRecommendationClient(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<List<string>> GetRecommendedProductsAsync(List<string> purchasedProducts, List<string> allDbProductNames)
        {
			
			string vaultUri = _config["AzureKeyVault:KeyVaultUrl"];
			var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());

			KeyVaultSecret secretAzureFunctionsRecommendrl = client.GetSecret("AzureFunctionsRecommendUrl");
            
			var functionUrl = secretAzureFunctionsRecommendrl.Value;  // Retrived from Azure Key Vault
            var functionKey = "";  // optional, depends on auth level

            var payload = new
            {
                purchasedProducts,
                allDbProductNames
            };

            var request = new HttpRequestMessage(HttpMethod.Post, functionUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrEmpty(functionKey))
            {
                request.Headers.Add("x-functions-key", functionKey);
            }

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
