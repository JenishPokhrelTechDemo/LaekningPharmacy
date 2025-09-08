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

        // List of product names recently purchased by users
        public List<string> PurchasedProducts { get; set; } = new();

        // List of recommended Product objects to display on the page
        public List<Product> RecommendedProducts { get; set; } = new();

        // Called when the page is requested via GET
        public async Task OnGetAsync()
        {
            // Get last 4 purchased products across all orders
            PurchasedProducts = _repository.Orders
                .Include(o => o.Lines) // include order lines
                .ThenInclude(l => l.Product) // include product details
                .OrderByDescending(o => o.OrderDate) // most recent orders first
                .SelectMany(o => o.Lines.Select(l => l.Product.Name)) // flatten to list of product names
                .Distinct()          // optional: avoid duplicates
                .Take(4)             // take only 4 recent purchases
                .ToList();

            // Fallback if no purchases exist (new store / no orders)
            if (!PurchasedProducts.Any())
            {
                // Pick 4 random products from the catalog
                PurchasedProducts = await _dbContext.Products
                    .OrderBy(p => Guid.NewGuid()) // random ordering
                    .Take(4)
                    .Select(p => p.Name)
                    .ToListAsync();
            }

            // Get all product names from the catalog
            var allProductNames = await _dbContext.Products
                .Select(p => p.Name)
                .ToListAsync();

            // Call Azure Function to get recommended product names
            var recommendedNames = await _recommendationClient
                .GetRecommendedProductsAsync(PurchasedProducts, allProductNames);

            // Map recommended product names back to Product objects
            RecommendedProducts = await _dbContext.Products
                .Where(p => recommendedNames.Contains(p.Name))
				.Take(5) // limit to 5 recommendations
                .ToListAsync();
        }
    }
}
