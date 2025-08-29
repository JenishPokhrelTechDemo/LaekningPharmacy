using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Laekning.Models;

public class IndexModel : PageModel
{
    private readonly StoreDbContext _context;

    public IndexModel(StoreDbContext context)
    {
        _context = context;
    }

    public IList<Product> Products { get; set; } = default!;
    
}
