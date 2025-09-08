using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Laekning.Models;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Laekning.Services;

// Helper class to correct OCR-extracted drug names using Azure OpenAI
public class OcrGptSearchHelper
{
    private readonly ChatClient _chatClient;   // Client for Azure OpenAI chat interactions
    private readonly StoreDbContext _dbContext; // Database context to access product data

    // Constructor: initializes Azure OpenAI chat client and database context
    public OcrGptSearchHelper(IConfiguration config, StoreDbContext dbContext)
    {
        // Get Key Vault URI from app configuration
        string vaultUri = config["AzureKeyVault:KeyVaultUrl"];
        
        // Create a Key Vault client using DefaultAzureCredential (supports managed identity)
        var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());

        // Retrieve Azure OpenAI endpoint, deployment name, and key from Key Vault
        KeyVaultSecret secretAzureOpenAIEndpoint = client.GetSecret("AzureOpenAIEndpoint");
        KeyVaultSecret secretAzureOpenAIDeploymentName = client.GetSecret("AzureOpenAIDeploymentName");
        KeyVaultSecret secretAzureOpenAIDeploymentKeyOne = client.GetSecret("AzureOpenAIDeploymentKeyOne");

        var endpoint = secretAzureOpenAIEndpoint.Value;
        var deployment = secretAzureOpenAIDeploymentName.Value;
        var key = secretAzureOpenAIDeploymentKeyOne.Value;

        // Initialize Azure OpenAI client
        var openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
        _chatClient = openAIClient.GetChatClient(deployment);

        // Store the database context
        _dbContext = dbContext;
    }

    // Correct OCR-extracted drug names using GPT
    public async Task<List<string>> GetCorrectedDrugNamesAsync(string ocrText, List<string> dbProductNames)
    {
        // Return empty list if OCR text or database product names are missing
        if (string.IsNullOrWhiteSpace(ocrText) || dbProductNames == null || !dbProductNames.Any())
            return new List<string>();

        // Combine all product names into a single string for GPT system prompt
        var productsListString = string.Join(", ", dbProductNames);

        // Prepare the messages for GPT chat model
        var messages = new List<ChatMessage>
        {
            // System message instructing GPT to act as a pharmacy assistant
            new SystemChatMessage(
                $"You are a pharmacy assistant. Match the provided OCR-extracted drug names to the closest exact matches from this list: {productsListString}. " +
                "Fix misspellings, handle partial names, and return only valid product names from the list. " +
                "Return names separated by commas without extra commentary."
            ),
            // User message containing the OCR text
            new UserChatMessage(ocrText)
        };

        // Stream the chat response from GPT
        var stream = _chatClient.CompleteChatStreamingAsync(messages);

        // Accumulate the streamed response text
        var responseText = new StringBuilder();
        await foreach (var update in stream)
        {
            foreach (var part in update.ContentUpdate)
            {
                responseText.Append(part.Text);
            }
        }

        // Split response into individual product names, trim whitespace, remove duplicates (case-insensitive)
        return responseText.ToString()
            .Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
