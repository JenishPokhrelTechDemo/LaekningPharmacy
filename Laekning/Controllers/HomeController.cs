using Microsoft.AspNetCore.Mvc;
using Laekning.Models;
using Laekning.Models.ViewModels;

namespace Laekning.Controllers {
    public class HomeController : Controller {
        private IStoreRepository repository;
        public int PageSize = 4;

        public HomeController(IStoreRepository repo) {
            repository = repo;
        }

        public IActionResult Index(string? category, int productPage = 1) {
            // If no category is selected, redirect to Recommendations
            if (string.IsNullOrEmpty(category)) {
                return RedirectToPage("/Recommendations"); 
                // or return RedirectToAction("Index", "Recommendations"); if Recommendations is also a controller
            }

            var viewModel = new ProductsListViewModel {
                Products = repository.Products
                    .Where(p => category == null || p.Category == category)
                    .OrderBy(p => p.ProductID)
                    .Skip((productPage - 1) * PageSize)
                    .Take(PageSize),
                PagingInfo = new PagingInfo {
                    CurrentPage = productPage,
                    ItemsPerPage = PageSize,
                    TotalItems = category == null
                        ? repository.Products.Count()
                        : repository.Products.Where(e => e.Category == category).Count()
                },
                CurrentCategory = category
            };

            return View(viewModel);
        }
    }
}
