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
        public List<Product> PurchasedProducts { get; set; } = new();

        // List of recommended Product objects to display on the page
        public List<Product> RecommendedProducts { get; set; } = new();

        // Called when the page is requested via GET       
		public async Task OnGetAsync()
		{
			// Get last 4 purchased products
			PurchasedProducts = _repository.Orders
				.Include(o => o.Lines)
				.ThenInclude(l => l.Product)
				.OrderByDescending(o => o.OrderDate)
				.SelectMany(o => o.Lines.Select(l => l.Product))
				.Distinct()
				.Take(4)
				.ToList();

			// Fallback if no purchases exist
			if (!PurchasedProducts.Any())
			{
				PurchasedProducts = await _dbContext.Products
					.OrderBy(p => Guid.NewGuid())
					.Take(4)
					.ToListAsync();
			}

			// Determine purchased categories
			var purchasedCategories = PurchasedProducts
				.Select(p => p.Category)
				.Distinct()
				.ToList();

			// Filter all products to only those in purchased categories
			var filteredProducts = await _dbContext.Products
				.Where(p => purchasedCategories.Contains(p.Category))
				.ToListAsync();

			// Call Azure Function with name + category
			var recommendedNames = await _recommendationClient.GetRecommendedProductsAsync(
				PurchasedProducts,
				filteredProducts
			);

			// Map recommended product names back to Product objects
			RecommendedProducts = filteredProducts
				.Where(p => recommendedNames.Contains(p.Name))
				.Take(5)
				.ToList();
		}

    }
}
