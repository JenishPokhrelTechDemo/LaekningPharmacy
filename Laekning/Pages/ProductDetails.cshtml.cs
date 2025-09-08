using Laekning.Models;                  // Import the application's models (Product, etc.)
using Microsoft.AspNetCore.Mvc;         // Provides IActionResult, Controller features
using Microsoft.AspNetCore.Mvc.RazorPages; // Provides PageModel base class for Razor Pages

namespace Laekning.Pages
{
    // PageModel class for the Product Details Razor Page
    public class ProductDetailsModel : PageModel
    {
        private readonly StoreDbContext _context; // Database context for accessing Products table

        // Constructor injection of the database context
        public ProductDetailsModel(StoreDbContext context)
        {
            _context = context;
        }

        // Property bound to the Razor Page to hold the product details
        [BindProperty]
        public Product? Product { get; set; } // Nullable because product may not exist

        // Handles GET requests to /ProductDetails/{id}
        public IActionResult OnGet(int id)
        {
            // Fetch product with matching ID from the database
            Product = _context.Products.FirstOrDefault(p => p.ProductID == id);

            if (Product == null) // If no product is found
            {
                return NotFound(); // Return 404 response
            }

            return Page(); // Render the Razor Page with Product property populated
        }
    }
}
