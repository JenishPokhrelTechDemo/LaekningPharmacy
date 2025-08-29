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

public class OcrGptSearchHelper
{
    private readonly ChatClient _chatClient;
    private readonly StoreDbContext _dbContext;
	
	public OcrGptSearchHelper(IConfiguration config, StoreDbContext dbContext)
    {
        string vaultUri = config["AzureKeyVault:KeyVaultUrl"];
        var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());

        KeyVaultSecret secretAzureOpenAIEndpoint = client.GetSecret("AzureOpenAIEndpoint");
        KeyVaultSecret secretAzureOpenAIDeploymentName =  client.GetSecret("AzureOpenAIDeploymentName");
        KeyVaultSecret secretAzureOpenAIDeploymentKeyOne = client.GetSecret("AzureOpenAIDeploymentKeyOne");

        var endpoint = secretAzureOpenAIEndpoint.Value;
        var deployment = secretAzureOpenAIDeploymentName.Value;
        var key = secretAzureOpenAIDeploymentKeyOne.Value;


        var openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
        _chatClient = openAIClient.GetChatClient(deployment);
        _dbContext = dbContext;
    }

    public async Task<List<string>> GetCorrectedDrugNamesAsync(string ocrText, List<string> dbProductNames)
        {
            if (string.IsNullOrWhiteSpace(ocrText) || dbProductNames == null || !dbProductNames.Any())
                return new List<string>();

            var productsListString = string.Join(", ", dbProductNames);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    $"You are a pharmacy assistant. Match the provided OCR-extracted drug names to the closest exact matches from this list: {productsListString}. " +
                    "Fix misspellings, handle partial names, and return only valid product names from the list. " +
                    "Return names separated by commas without extra commentary."
                ),
                new UserChatMessage(ocrText)
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

            return responseText.ToString()
                .Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
}
