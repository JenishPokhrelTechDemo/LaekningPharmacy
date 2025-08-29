using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;


namespace Laekning.Models {

    public class Product {
		
        public long? ProductID { get; set; }

		[Required(ErrorMessage = "Please enter a product name")]
		[SearchableField]
        public string Name { get; set; } = String.Empty;
		
		[Required(ErrorMessage = "Please enter a description")]
        public string Description { get; set; } = String.Empty;

		[Required]
		[Range(0.01, double.MaxValue, ErrorMessage = "Please enter a positive price")]
		[Column(TypeName = "decimal(8,2)")]
        public decimal Price { get; set; }

		[Required(ErrorMessage = "Please specify a category")]
		[SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string Category { get; set; } = String.Empty;
		
		public string? Image { get; set; }
	
    }
}



