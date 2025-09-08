using Microsoft.AspNetCore.Mvc;
using Laekning.Models;
using Laekning.Models.ViewModels;

namespace Laekning.Controllers {
	//Homecontroller handles product browsing and category filtering
    public class HomeController : Controller {
		//Repository gives access to Products (from Database or other modes of storage)
        private IStoreRepository repository;
		
		//No. of products to show per page
        public int PageSize = 4;

		//Constructor to inject IStoreRepository dependency
        public HomeController(IStoreRepository repo) {
            repository = repo;
        }

		//Main action to display products
		//Parameters:
		//category - category filter (nullable, defaults to all if null)
		//   productPage - current page number (defaults to 1)
        public IActionResult Index(string? category, int productPage = 1) {
            // If no category is selected, redirect to Recommendations
            if (string.IsNullOrEmpty(category)) {
                return RedirectToPage("/Recommendations"); 
                // or return RedirectToAction("Index", "Recommendations"); if Recommendations is also a controller
            }
			//ViewModel to be built with Products and Pagination Info
            var viewModel = new ProductsListViewModel {
				// Products to be filtered by category, sorted by ProductID, then paginated
                Products = repository.Products
                    .Where(p => category == null || p.Category == category)
                    .OrderBy(p => p.ProductID)
                    .Skip((productPage - 1) * PageSize)  // Skip previous pages
                    .Take(PageSize),                     // Take only PageSize items

				
				// Create pagination details for the view
                PagingInfo = new PagingInfo {
                    CurrentPage = productPage,
                    ItemsPerPage = PageSize,
                    TotalItems = category == null
                        ? repository.Products.Count() //Count all products
                        : repository.Products.Where(e => e.Category == category).Count() // Count filtered products
                },
				// Keep track of the current category filter
                CurrentCategory = category
            };
			
			// Pass the ViewModel to the view
            return View(viewModel);
        }
    }
}
