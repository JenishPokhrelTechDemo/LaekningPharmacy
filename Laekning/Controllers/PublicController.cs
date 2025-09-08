using Microsoft.AspNetCore.Mvc;
using Laekning.Models;
using Laekning.Services; 
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json; // used for JSON serialization

namespace Laekning.Controllers
{
    public class PublicController : Controller
    {
        // Database context for querying products
        private readonly StoreDbContext _context;

        // Service that uses GPT to recommend products
        private readonly GptProductRecommender _gptService;

        // Session key for storing chat history
        private const string SessionKeyChatHistory = "HealthAssistantChatHistory";

        // Dependencies are injected through constructor
        public PublicController(StoreDbContext context, GptProductRecommender gptService)
        {
            _context = context;
            _gptService = gptService;
        }

        // GET: Display Health Assistant chat UI
        [HttpGet]
        [Route("HealthAssistant")]
        public IActionResult HealthAssistant()
        {
            var chatHistory = GetChatHistoryFromSession();
            return View(chatHistory);
        }

        // POST: Handle user query and return updated chat
        [HttpPost]
        [Route("HealthAssistant")]
        public async Task<IActionResult> HealthAssistant(string query)
        {
            // If user input is empty, reload the page
            if (string.IsNullOrWhiteSpace(query))
                return RedirectToAction("HealthAssistant");

            var chatHistory = GetChatHistoryFromSession();

            // Add user message to chat history
            chatHistory.Add(new ChatMessage
            {
                Role = "user",
                Content = query
            });

            // Ask GPT for product recommendations
            var recommendedProducts = await _gptService.GetRecommendedProductsAsync(query);

            // Extract product names suggested by GPT
            var recommendedNames = recommendedProducts.Select(r => r.ProductName).ToList();

            // Find matching products in the database
            var dbProducts = _context.Products
                .Where(p => recommendedNames.Contains(p.Name))
                .ToList();

            var botResponses = new List<string>();

            // Build bot responses for each recommendation
            foreach (var rec in recommendedProducts)
            {
                if (rec.Available)
                {
                    // Try to find matching product in DB for redirect link
                    var matchedProduct = dbProducts.FirstOrDefault(p => p.Name == rec.ProductName);
                    var redirectUrl = matchedProduct != null
                        ? Url.Page("/ProductDetails", new { id = matchedProduct.ProductID })
                        : "#";

                    botResponses.Add($"You may try <strong>{rec.ProductName}</strong>. You can view it <a href='{redirectUrl}'>here</a>.");
                }
                else
                {
                    // Product not available in stock
                    botResponses.Add($"We currently do not have <strong>{rec.ProductName}</strong> in stock, but we’ll consider adding it soon.");
                }
            }

            // Add bot responses to chat history
            foreach (var response in botResponses)
            {
                chatHistory.Add(new ChatMessage
                {
                    Role = "bot",
                    Content = response
                });
            }

            // Save updated chat history back to session
            SaveChatHistoryToSession(chatHistory);

            // Render updated chat in view
            return View("HealthAssistant", chatHistory);
        }

        // Helper: Retrieve chat history from session (deserialize JSON)
        private List<ChatMessage> GetChatHistoryFromSession()
        {
            var json = HttpContext.Session.GetString(SessionKeyChatHistory);
            if (string.IsNullOrEmpty(json))
                return new List<ChatMessage>();

            return JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new List<ChatMessage>();
        }

        // Helper: Save chat history to session (serialize JSON)
        private void SaveChatHistoryToSession(List<ChatMessage> chatHistory)
        {
            var json = JsonSerializer.Serialize(chatHistory);
            HttpContext.Session.SetString(SessionKeyChatHistory, json);
        }
    }

    // Simple model for chat messages
    public class ChatMessage
    {
        public string Role { get; set; } // "user" or "bot"
        public string Content { get; set; }
    }
}
