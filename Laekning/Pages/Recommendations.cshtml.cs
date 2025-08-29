using Laekning.Models;
using Laekning.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Laekning.Pages
{
    public class RecommendationsModel : PageModel
    {
        private readonly IOrderRepository _repository;
        private readonly StoreDbContext _dbContext;
        private readonly AzureFunctionRecommendationClient _recommendationClient;

        public RecommendationsModel(
            IOrderRepository repository,
            StoreDbContext dbContext,
            AzureFunctionRecommendationClient recommendationClient)
        {
            _repository = repository;
            _dbContext = dbContext;
            _recommendationClient = recommendationClient;
        }

        public List<string> PurchasedProducts { get; set; } = new();
        public List<Product> RecommendedProducts { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Get last 4 purchased products across all orders
            PurchasedProducts = _repository.Orders
                .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
                .OrderByDescending(o => o.OrderDate)
                .SelectMany(o => o.Lines.Select(l => l.Product.Name))
                .Distinct()          // optional: avoid duplicates
                .Take(4)
                .ToList();

            // Fallback if no purchases exist
            if (!PurchasedProducts.Any())
            {
                PurchasedProducts = await _dbContext.Products
                    .OrderBy(p => Guid.NewGuid())
                    .Take(4)
                    .Select(p => p.Name)
                    .ToListAsync();
            }

            // Get all product names
            var allProductNames = await _dbContext.Products
                .Select(p => p.Name)
                .ToListAsync();

            // Call Azure Function for recommendations
            var recommendedNames = await _recommendationClient
                .GetRecommendedProductsAsync(PurchasedProducts, allProductNames);

            // Map recommended names back to Product objects
            RecommendedProducts = await _dbContext.Products
                .Where(p => recommendedNames.Contains(p.Name))
				.Take(5)
                .ToListAsync();
        }
    }
}
