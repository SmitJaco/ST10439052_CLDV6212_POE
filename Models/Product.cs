using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace ST10439052_CLDV_POE.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;

        [Display(Name = "Product ID")]
        public string ProductId => RowKey;

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        // Keep PriceString for backward compatibility with Azure Table Storage
        [Display(Name = "Price String")]
        public string PriceString
        {
            get => Price.ToString("F2");
            set => Price = decimal.TryParse(value, out var result) ? result : 0m;
        }

        [Required]
        [Display(Name = "Stock Available")]
        public int StockAvailable { get; set; }

        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = string.Empty;

        // Optional: Custom updated timestamp for business logic
        [Display(Name = "Last Updated")]
        public DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
