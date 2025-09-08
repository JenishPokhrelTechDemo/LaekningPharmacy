using Microsoft.AspNetCore.Mvc.RazorPages; // Provides PageModel base class for Razor Pages
using Microsoft.EntityFrameworkCore;       // Provides Entity Framework Core features (e.g., DbContext, LINQ queries)
using System.Collections.Generic;          // Provides IList<T>
using System.Linq;                         // Provides LINQ extension methods
using System.Threading.Tasks;              // Provides Task for asynchronous operations
using Laekning.Models;                     // Access your application models, e.g., Product

// Razor Page model for the Index page
public class IndexModel : PageModel
{
    // Private field for the database context (used to access the Products table)
    private readonly StoreDbContext _context;

    // Constructor receives a StoreDbContext via dependency injection
    public IndexModel(StoreDbContext context)
    {
        _context = context;
    }

    // Public property to hold a list of products to display on the Index page
    public IList<Product> Products { get; set; } = default!; // 'default!' used to suppress null warning
}
