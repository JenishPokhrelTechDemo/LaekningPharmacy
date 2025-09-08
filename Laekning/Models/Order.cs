using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Laekning.Models {

    // Represents a customer order
    public class Order {
        // Order ID (primary key), not bound to incoming request data
        [BindNever]
        public int OrderID { get; set; }

        // Collection of cart lines included in this order, not bound to incoming request
        [BindNever]
        public ICollection<CartLine> Lines { get; set; } = new List<CartLine>();

        // Customer's name (required for validation)
        [Required(ErrorMessage = "Please enter a name")]
        public string? Name { get; set; }

        // Address line 1 (required)
        [Required(ErrorMessage = "Please enter the first address line")]
        public string? Line1 { get; set; }

        // Optional address lines 2 and 3
        public string? Line2 { get; set; }
        public string? Line3 { get; set; }

        // City name (required)
        [Required(ErrorMessage = "Please enter a city name")]
        public string? City { get; set; }

        // State name (required)
        [Required(ErrorMessage = "Please enter a state name")]
        public string? State { get; set; }

        // Zip/postal code (optional)
        public string? Zip { get; set; }

        // Country name (required)
        [Required(ErrorMessage = "Please enter a country name")]
        public string? Country { get; set; }

        // Whether the order should be gift wrapped
        public bool GiftWrap { get; set; }

        // Shipping status, not bound to incoming request
        [BindNever]
        public bool Shipped { get; set; }

        // Date and time when the order was created (default to UTC now)
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    }
}
