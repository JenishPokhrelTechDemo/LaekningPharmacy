namespace Laekning.Models.ViewModels {

    // ViewModel to hold paging information for paginated lists
    public class PagingInfo {
        // Total number of items in the collection
        public int TotalItems { get; set; }

        // Number of items to display per page
        public int ItemsPerPage { get; set; }

        // Current page number being displayed
        public int CurrentPage { get; set; }

        // Total number of pages calculated based on total items and items per page
        public int TotalPages =>
            (int)Math.Ceiling((decimal)TotalItems / ItemsPerPage);
    }
}
