using Laekning.Models;
using Laekning.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Laekning.Pages
{
    public class RecommendationsModel : PageModel
    {
        // Dependencies for data access and recommendations
        private readonly IOrderRepository _repository; // to access orders
        private readonly StoreDbContext _dbContext; // EF Core context for products
        private readonly AzureFunctionRecommendationClient _recommendationClient; // Azure Function for product recommendations

        // Constructor injection for dependencies
        public RecommendationsModel(
            IOrderRepository repository,
            StoreDbContext dbContext,
            AzureFunctionRecommendationClient recommendationClient)
        {
            _repository = repository;
            _dbContext = dbContext;
            _recommendationClient = recommendationClient;
        }

        // List of description of products recently purchased by users
        public List<string> PurchasedProductDescriptions { get; set; } = new();

        // List of recommended Product objects to display on the page
        public List<Product> RecommendedProducts { get; set; } = new();

        // Called when the page is requested via GET
        public async Task OnGetAsync()
        {
            // Get last 4 purchased product categories across all orders
            PurchasedProductDescriptions = _repository.Orders
                .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
                .OrderByDescending(o => o.OrderDate)
                .SelectMany(o => o.Lines.Select(l => l.Product.Description))
                .Take(4) // take 4 most recent categories
                .Distinct() // optional, remove duplicates
                .ToList();


            // Fallback if no orders exist
            if (!PurchasedProductDescriptions.Any())
            {
                PurchasedProductDescriptions = await _dbContext.Products
                    .OrderBy(p => Guid.NewGuid())
                    .Take(4)
                    .Select(p => p.Description)
                    .ToListAsync();
            }


            // Get all product names from the catalog
            var allProductNames = await _dbContext.Products
                .Select(p => p.Name)
                .ToListAsync();

            // Call Azure Function to get recommended product names
            var recommendedNames = await _recommendationClient
                .GetRecommendedProductsAsync(PurchasedProductDescriptions, allProductNames);

            // Map recommended product names back to Product objects
            RecommendedProducts = await _dbContext.Products
                .Where(p => recommendedNames.Contains(p.Name))
				.Take(5) // limit to 5 recommendations
                .ToListAsync();
        }
    }
}

