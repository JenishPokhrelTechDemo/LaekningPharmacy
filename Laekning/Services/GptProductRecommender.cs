using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Laekning.Models; 
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;


namespace Laekning.Services
{
    public class ProductRecommendationResult
    {
        public string ProductName { get; set; }
        public bool Available { get; set; }
    }

    public class GptProductRecommender
    {
        private readonly ChatClient _chatClient;
        private readonly StoreDbContext _dbContext;

		
		public GptProductRecommender(IConfiguration config, StoreDbContext dbContext)
        {
            string vaultUri = config["AzureKeyVault:KeyVaultUrl"];
			var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());

			KeyVaultSecret secretAzureOpenAIEndpoint = client.GetSecret("AzureOpenAIEndpoint");
			KeyVaultSecret secretAzureOpenAIDeploymentName = client.GetSecret("AzureOpenAIDeploymentName");
			KeyVaultSecret secretAzureOpenAIDeploymentKeyOne = client.GetSecret("AzureOpenAIDeploymentKeyOne");

			var endpoint = secretAzureOpenAIEndpoint.Value;
			var deployment = secretAzureOpenAIDeploymentName.Value;
			var key = secretAzureOpenAIDeploymentKeyOne.Value;
			
			if (string.IsNullOrWhiteSpace(endpoint) ||
                string.IsNullOrWhiteSpace(deployment) ||
                string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("Azure OpenAI configuration is missing or incomplete.");
            }

            var openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
            _chatClient = openAIClient.GetChatClient(deployment);
            _dbContext = dbContext;
        }


        public async Task<List<ProductRecommendationResult>> GetRecommendedProductsAsync(string userQuery)
		{
			
			var allDbProductNames = _dbContext.Products.Select(p => p.Name).ToList();
			var productsListString = string.Join(", ", allDbProductNames);
					
			var messages = new List<ChatMessage>
				{
					    new SystemChatMessage(
							$"You're a helpful pharmacy assistant. Suggest relevant pharmacy products based on user symptoms or medication queries. Return only exact product names from this list: {productsListString}. Return names separated by commas. And finally, limit the answers to maximum of 350 words."
						),
					new UserChatMessage(userQuery)
				};

			var stream = _chatClient.CompleteChatStreamingAsync(messages);

			var responseText = new StringBuilder();

			await foreach (var update in stream)
			{
				foreach (var part in update.ContentUpdate)
				{
					responseText.Append(part.Text);
				}
			}
		
			var content = responseText.ToString();

			var productNames = content
				.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(p => p.Trim())
				.Where(p => !string.IsNullOrWhiteSpace(p))
				.ToList();

			var results = new List<ProductRecommendationResult>();

			foreach (var name in productNames)
			{
				bool available = await _dbContext.Products
					.AnyAsync(p =>
					EF.Functions.Like(p.Name, $"%{name}%") ||
					EF.Functions.Like(p.Description, $"%{name}%")
				);

				results.Add(new ProductRecommendationResult
				{
					ProductName = name,
					Available = available
				});
			}

		return results;
		}
    }
}
