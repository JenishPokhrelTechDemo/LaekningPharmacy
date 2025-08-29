using Laekning.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laekning.Pages
{
    public class ProductDetailsModel : PageModel
    {
        private readonly StoreDbContext _context;

        public ProductDetailsModel(StoreDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Product? Product { get; set; }

        public IActionResult OnGet(int id)
        {
            Product = _context.Products.FirstOrDefault(p => p.ProductID == id);
            if (Product == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}
