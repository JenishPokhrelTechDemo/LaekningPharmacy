using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Laekning.Models;
using Laekning.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Laekning.Services;
using System;

namespace Laekning.Pages
{
    public class SearchResultsModel : PageModel
    {
        private readonly StoreDbContext _dbContext;
        private readonly OcrGptSearchHelper _gptHelper;
		private readonly EventHubSender _eventHub;

        public SearchResultsModel(StoreDbContext dbContext, OcrGptSearchHelper gptHelper, EventHubSender eventHub)
        {
            _dbContext = dbContext;
            _gptHelper = gptHelper;
			_eventHub = eventHub;
			
        }

        [BindProperty(SupportsGet = true)]
        public string ExtractedInscription { get; set; }

        public List<Product> ProductsList { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(ExtractedInscription))
            {
                ProductsList = new List<Product>();
                return Page();
            }

            // Get all product names from DB
            var dbProductNames = await _dbContext.Products
                .Select(p => p.Name)
                .ToListAsync();

            // Get corrected matches from Azure OpenAI
            var correctedMatches = await _gptHelper.GetCorrectedDrugNamesAsync(ExtractedInscription, dbProductNames);

            if (!correctedMatches.Any())
            {
                ProductsList = new List<Product>();
                return Page();
            }

            // Query DB for all matches
            ProductsList = await _dbContext.Products
                .Where(p => correctedMatches.Contains(p.Name))
                .ToListAsync();
			
			 // Send "ProductsIdentified" event
            var productsEvent = new
            {
                EventType = "ProductsIdentified",
                ExtractedInscription,
                IdentifiedProducts = ProductsList.Select(p => new { p.Name, p.Category, p.Price }),
                Timestamp = DateTime.UtcNow,
                ProcessedBy = "OcrGptSearchHelper"
            };
            await _eventHub.SendAsync(productsEvent);


            return Page();
        }
    }
}
