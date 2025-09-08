using System.Text.Json.Serialization;
using Laekning.Infrastructure;

namespace Laekning.Models {

    // Represents a shopping cart stored in the user's session
    public class SessionCart : Cart {

        // Retrieves the current cart from session or creates a new one
        public static Cart GetCart(IServiceProvider services) {
            // Get the current session from the HTTP context
            ISession? session =
                services.GetRequiredService<IHttpContextAccessor>()
                        .HttpContext?.Session;

            // Try to get the cart from session; create new if not present
            SessionCart cart = session?.GetJson<SessionCart>("Cart")
                                ?? new SessionCart();

            // Attach the session to the cart for later updates
            cart.Session = session;

            return cart;
        }

        // The HTTP session associated with this cart (ignored in JSON serialization)
        [JsonIgnore]
        public ISession? Session { get; set; }

        // Adds a product to the cart and updates the session
        public override void AddItem(Product product, int quantity) {
            base.AddItem(product, quantity);
            Session?.SetJson("Cart", this);
        }

        // Removes a product line from the cart and updates the session
        public override void RemoveLine(Product product) {
            base.RemoveLine(product);
            Session?.SetJson("Cart", this);
        }

        // Clears the cart and removes it from the session
        public override void Clear() {
            base.Clear();
            Session?.Remove("Cart");
        }
    }
}
