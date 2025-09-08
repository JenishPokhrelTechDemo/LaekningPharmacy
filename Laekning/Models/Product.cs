using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// The Azure AI Search-related classes are retained here for potential future use. 
// Originally, the project planned to use Azure AI Search along with the Document Intelligence OCR service 
// to identify drugs in prescription images. This approach was replaced with Generative AI-powered OCR, 
// but the code is kept in case Azure AI Search could improve the service in the future.

using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace Laekning.Models {

    // Represents a product in the store
    public class Product {

        // Product ID (primary key, nullable for new products)
        public long? ProductID { get; set; }

        // Name of the product (required, searchable in Azure Search)
        [Required(ErrorMessage = "Please enter a product name")]
        [SearchableField]
        public string Name { get; set; } = String.Empty;

        // Description of the product (required)
        [Required(ErrorMessage = "Please enter a description")]
        public string Description { get; set; } = String.Empty;

        // Price of the product (required, must be positive)
        // Stored as decimal(8,2) in the database
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Please enter a positive price")]
        [Column(TypeName = "decimal(8,2)")]
        public decimal Price { get; set; }

        // Category of the product (required, searchable and filterable in Azure Search)
        [Required(ErrorMessage = "Please specify a category")]
        [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string Category { get; set; } = String.Empty;

        // Optional image file name or URL for the product
        public string? Image { get; set; }
    }
}
