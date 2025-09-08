using Microsoft.AspNetCore.Mvc;
using Laekning.Models;

namespace Laekning.Components {

    // ViewComponent to generate the navigation menu (list of product categories)
    public class NavigationMenuViewComponent : ViewComponent {
        // Repository to access products
        private IStoreRepository repository;

        // Constructor injects the repository dependency
        public NavigationMenuViewComponent(IStoreRepository repo) {
            repository = repo;
        }

        // This method is called when the ViewComponent is rendered
        public IViewComponentResult Invoke() {
            // Store the currently selected category in ViewBag
            ViewBag.SelectedCategory = RouteData?.Values["category"];

            // Get all unique product categories, sort them alphabetically, and pass to the view
            return View(repository.Products
                .Select(x => x.Category)
                .Distinct()
                .OrderBy(x => x));
        }
    }
}
