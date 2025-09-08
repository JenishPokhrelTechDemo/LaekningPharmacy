namespace Laekning.Models {

    // Represents a shopping cart containing multiple cart lines
    public class Cart {
        // List of items (lines) in the cart
        public List<CartLine> Lines { get; set; } = new List<CartLine>();

        // Add a product to the cart or update quantity if it already exists
        public virtual void AddItem(Product product, int quantity) {
            // Try to find an existing cart line for the same product
            CartLine? line = Lines
                .Where(p => p.Product.ProductID == product.ProductID)
                .FirstOrDefault();

            if (line == null) {
                // If not found, add a new line with the product and quantity
                Lines.Add(new CartLine {
                    Product = product,
                    Quantity = quantity
                });
            } else {
                // If found, increment the quantity
                line.Quantity += quantity;
            }
        }

        // Remove a product completely from the cart
        public virtual void RemoveLine(Product product) =>
            Lines.RemoveAll(l => l.Product.ProductID == product.ProductID);

        // Compute the total value of all items in the cart
        public decimal ComputeTotalValue() =>
            Lines.Sum(e => e.Product.Price * e.Quantity);

        // Clear all items from the cart
        public virtual void Clear() => Lines.Clear();
    }

    // Represents a single line/item in the shopping cart
    public class CartLine {
        public int CartLineID { get; set; }          // Unique ID for the cart line
        public Product Product { get; set; } = new(); // The product associated with this line
        public int Quantity { get; set; }            // Quantity of the product
    }
}
