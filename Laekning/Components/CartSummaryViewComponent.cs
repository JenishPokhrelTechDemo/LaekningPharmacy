using Microsoft.AspNetCore.Mvc;
using Laekning.Models;

namespace Laekning.Components {

    // ViewComponent to display a summary of the shopping cart
    public class CartSummaryViewComponent : ViewComponent {
        // Shopping cart instance for the current user/session
        private Cart cart;

        // Constructor injects the Cart dependency
        public CartSummaryViewComponent(Cart cartSummary) {
            cart = cartSummary;
        }

        // Called when the ViewComponent is rendered
        public IViewComponentResult Invoke() {
            // Pass the cart object to the view for rendering
            return View(cart);
        }
    }
}
