using Microsoft.AspNetCore.Mvc;
using Laekning.Models;
using Laekning.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json; // for JSON serialization

namespace Laekning.Controllers
{
    public class PublicController : Controller
    {
        private readonly StoreDbContext _context;
        private readonly GptProductRecommender _gptService;

        private const string SessionKeyChatHistory = "HealthAssistantChatHistory";

        public PublicController(StoreDbContext context, GptProductRecommender gptService)
        {
            _context = context;
            _gptService = gptService;
        }

        [HttpGet]
        [Route("HealthAssistant")]
        public IActionResult HealthAssistant()
        {
            var chatHistory = GetChatHistoryFromSession();
            return View(chatHistory);
        }

        [HttpPost]
        [Route("HealthAssistant")]
        public async Task<IActionResult> HealthAssistant(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return RedirectToAction("HealthAssistant");

            var chatHistory = GetChatHistoryFromSession();

            // Add user message to history
            chatHistory.Add(new ChatMessage
            {
                Role = "user",
                Content = query
            });

            // Ask GPT for recommendations
            var recommendedProducts = await _gptService.GetRecommendedProductsAsync(query);

            var recommendedNames = recommendedProducts.Select(r => r.ProductName).ToList();
            var dbProducts = _context.Products
                .Where(p => recommendedNames.Contains(p.Name))
                .ToList();

            var botResponses = new List<string>();

            foreach (var rec in recommendedProducts)
            {
                if (rec.Available)
                {
                    var matchedProduct = dbProducts.FirstOrDefault(p => p.Name == rec.ProductName);
                    var redirectUrl = matchedProduct != null
                        ? Url.Page("/ProductDetails", new { id = matchedProduct.ProductID })
                        : "#";

                    botResponses.Add($"You may try <strong>{rec.ProductName}</strong>. You can view it <a href='{redirectUrl}'>here</a>.");
                }
                else
                {
                    botResponses.Add($"We currently do not have <strong>{rec.ProductName}</strong> in stock, but we’ll consider adding it soon.");
                }
            }

            // Add bot responses to history
            foreach (var response in botResponses)
            {
                chatHistory.Add(new ChatMessage
                {
                    Role = "bot",
                    Content = response
                });
            }

            // Save back to session
            SaveChatHistoryToSession(chatHistory);

            return View("HealthAssistant", chatHistory);
        }

        // Helper: Get chat history from session
        private List<ChatMessage> GetChatHistoryFromSession()
        {
            var json = HttpContext.Session.GetString(SessionKeyChatHistory);
            if (string.IsNullOrEmpty(json))
                return new List<ChatMessage>();

            return JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new List<ChatMessage>();
        }

        // Helper: Save chat history to session
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
