using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace ST10439052_CLDV_POE.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;

        [Display(Name = "Order ID")]
        public string OrderId => RowKey;

        [Required]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty;

        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product")]
        public string ProductId { get; set; } = string.Empty;

        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Display(Name = "Unit Price")]
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Total Price")]
        [DataType(DataType.Currency)]
        public decimal TotalPrice { get; set; }

        // String representation for Azure Table Storage compatibility
        [Display(Name = "Total Price String")]
        public string TotalPriceString
        {
            get => TotalPrice.ToString("F2");
            set => TotalPrice = decimal.TryParse(value, out var result) ? result : 0m;
        }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Submitted";

        public enum OrderStatus
        {
            Submitted,
            Processing,
            Completed,
            Cancelled
        }

        // Optional: Custom updated timestamp for business logic
        [Display(Name = "Last Updated")]
        public DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
