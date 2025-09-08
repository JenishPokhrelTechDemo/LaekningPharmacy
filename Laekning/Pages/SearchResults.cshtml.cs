using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Laekning.Models;
using Laekning.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Laekning.Pages
{
    public class SearchResultsModel : PageModel
    {
        // Database context for accessing Products table
        private readonly StoreDbContext _dbContext;

        // Helper service to correct OCR text using Azure OpenAI GPT
        private readonly OcrGptSearchHelper _gptHelper;

        // EventHub service for logging events asynchronously
        private readonly EventHubSender _eventHub;

        // Constructor with dependency injection
        public SearchResultsModel(StoreDbContext dbContext, OcrGptSearchHelper gptHelper, EventHubSender eventHub)
        {
            _dbContext = dbContext;
            _gptHelper = gptHelper;
            _eventHub = eventHub;
        }

        // Bound property to get the extracted prescription text from query string
        [BindProperty(SupportsGet = true)]
        public string ExtractedInscription { get; set; }

        // List to store products that match the extracted prescription
        public List<Product> ProductsList { get; set; } = new();

        // Handler method for GET requests
        public async Task<IActionResult> OnGetAsync()
        {
            // If no search term is provided, return empty list
            if (string.IsNullOrWhiteSpace(ExtractedInscription))
            {
                ProductsList = new List<Product>();
                return Page();
            }

            // Get all product names from the database
            var dbProductNames = await _dbContext.Products
                .Select(p => p.Name)
                .ToListAsync();

            // Use Azure OpenAI GPT helper to correct OCR-extracted drug names
            var correctedMatches = await _gptHelper.GetCorrectedDrugNamesAsync(ExtractedInscription, dbProductNames);

            // If no matches found, return empty list
            if (!correctedMatches.Any())
            {
                ProductsList = new List<Product>();
                return Page();
            }

            // Query database for products whose names match the corrected list
            ProductsList = await _dbContext.Products
                .Where(p => correctedMatches.Contains(p.Name))
                .ToListAsync();

            // Send an event to EventHub for logging/search tracking
            var productsEvent = new
            {
                EventType = "ProductsIdentified",
                ExtractedInscription, // Original OCR text
                IdentifiedProducts = ProductsList.Select(p => new { p.Name, p.Category, p.Price }),
                Timestamp = DateTime.UtcNow,
                ProcessedBy = "OcrGptSearchHelper" // Service that processed the OCR
            };
            await _eventHub.SendAsync(productsEvent);

            // Return the page with ProductsList populated
            return Page();
        }
    }
}
