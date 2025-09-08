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
    // Represents the result of a product recommendation
    public class ProductRecommendationResult
    {
        public string ProductName { get; set; }  // Name of the recommended product
        public bool Available { get; set; }      // Whether the product exists in the database
    }

    // Service class to get product recommendations from Azure OpenAI
    public class GptProductRecommender
    {
        private readonly ChatClient _chatClient;  // Client for streaming chat completions
        private readonly StoreDbContext _dbContext; // Database context for product availability

        // Constructor: initializes OpenAI client and database context
        public GptProductRecommender(IConfiguration config, StoreDbContext dbContext)
        {
            // Retrieve Key Vault URI from configuration
            string vaultUri = config["AzureKeyVault:KeyVaultUrl"];
            
            // Create a Key Vault client using DefaultAzureCredential (supports managed identity)
            var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());

            // Retrieve Azure OpenAI configuration from Key Vault
            KeyVaultSecret secretAzureOpenAIEndpoint = client.GetSecret("AzureOpenAIEndpoint");
            KeyVaultSecret secretAzureOpenAIDeploymentName = client.GetSecret("AzureOpenAIDeploymentName");
            KeyVaultSecret secretAzureOpenAIDeploymentKeyOne = client.GetSecret("AzureOpenAIDeploymentKeyOne");

            var endpoint = secretAzureOpenAIEndpoint.Value;
            var deployment = secretAzureOpenAIDeploymentName.Value;
            var key = secretAzureOpenAIDeploymentKeyOne.Value;

            // Validate configuration
            if (string.IsNullOrWhiteSpace(endpoint) ||
                string.IsNullOrWhiteSpace(deployment) ||
                string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("Azure OpenAI configuration is missing or incomplete.");
            }

            // Initialize Azure OpenAI client and chat client for the specified deployment
            var openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
            _chatClient = openAIClient.GetChatClient(deployment);

            // Store database context
            _dbContext = dbContext;
        }

        // Get product recommendations for a given user query
        public async Task<List<ProductRecommendationResult>> GetRecommendedProductsAsync(string userQuery)
        {
            // Get all product names from the database
            var allDbProductNames = _dbContext.Products.Select(p => p.Name).ToList();
            var productsListString = string.Join(", ", allDbProductNames);

            // Create messages for OpenAI chat model
            var messages = new List<ChatMessage>
            {
                // System prompt: instructs the AI to act as a pharmacy assistant
                new SystemChatMessage(
                    $"You're a helpful pharmacy assistant. Suggest relevant pharmacy products based on user symptoms or medication queries. Return only exact product names from this list: {productsListString}. Return names separated by commas. And finally, limit the answers to maximum of 350 words."
                ),
                // User message containing the query
                new UserChatMessage(userQuery)
            };

            // Stream response from OpenAI chat client
            var stream = _chatClient.CompleteChatStreamingAsync(messages);

            var responseText = new StringBuilder();

            await foreach (var update in stream)
            {
                // Append each chunk of content as it arrives
                foreach (var part in update.ContentUpdate)
                {
                    responseText.Append(part.Text);
                }
            }

            var content = responseText.ToString();

            // Parse response into individual product names
            var productNames = content
                .Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            var results = new List<ProductRecommendationResult>();

            // Check database for availability of each recommended product
            foreach (var name in productNames)
            {
                bool available = await _dbContext.Products
                    .AnyAsync(p =>
                        EF.Functions.Like(p.Name, $"%{name}%") ||       // Matches product name
                        EF.Functions.Like(p.Description, $"%{name}%")   // Matches description
                    );

                results.Add(new ProductRecommendationResult
                {
                    ProductName = name,
                    Available = available
                });
            }

            return results; // Return list of recommended products with availability
        }
    }
}
