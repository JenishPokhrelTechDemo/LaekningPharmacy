using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Laekning.Infrastructure;
using Laekning.Models;

namespace Laekning.Pages {

    // PageModel for the Cart page — handles adding/removing items 
    // and maintaining the cart state.
    public class CartModel : PageModel {
        private IStoreRepository repository;

        // Constructor: gets the repository for product data
        // and the Cart service (injected via dependency injection).
        public CartModel(IStoreRepository repo, Cart cartService) {
            repository = repo;
			Cart = cartService;
        }

        // The Cart instance (shared session-backed cart).
        public Cart Cart { get; set; }

        // Return URL — allows redirecting back to the page user came from.
        public string ReturnUrl { get; set; } = "/";

        // Handles GET requests — sets the return URL.
        // (Commented-out code shows older session-based cart retrieval.)
        public void OnGet(string returnUrl) {
            ReturnUrl = returnUrl ?? "/";
            //Cart = HttpContext.Session.GetJson<Cart>("cart") 
            //    ?? new Cart();
        }

        // Handles POST requests to add a product to the cart.
        public IActionResult OnPost(long productId, string returnUrl) {
            // Find product in repository
            Product? product = repository.Products
                .FirstOrDefault(p => p.ProductID == productId);

            // If found, add one unit to the cart
            if (product != null) {
                Cart.AddItem(product, 1);
            }

            // Redirect back to cart page with correct return URL
            return RedirectToPage(new { returnUrl = returnUrl });
        }
		
		// Handles POST requests to remove a product line from the cart.
		public IActionResult OnPostRemove(long productId, string returnUrl) {
            // Find the cart line for the given product and remove it
            Cart.RemoveLine(Cart.Lines.First(cl =>
				cl.Product.ProductID == productId).Product);

            // Redirect back to cart page with correct return URL
            return RedirectToPage(new { returnUrl = returnUrl });
        }
    }
}
