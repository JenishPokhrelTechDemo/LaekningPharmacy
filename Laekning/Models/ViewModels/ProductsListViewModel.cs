namespace Laekning.Models.ViewModels {

    // ViewModel used to display a list of products with pagination and category info
    public class ProductsListViewModel {

        // Collection of products to be displayed on the current page
        public IEnumerable<Product> Products { get; set; }
            = Enumerable.Empty<Product>();

        // Pagination information for the product list (current page, total pages, etc.)
        public PagingInfo PagingInfo { get; set; } = new();

        // The category currently being viewed or filtered (nullable)
        public string? CurrentCategory { get; set; }
    }
}
