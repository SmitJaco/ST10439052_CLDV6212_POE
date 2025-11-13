namespace ST10439052_CLDV_POE.Models.ViewModels
{
    public class CartItemViewModel
    {
        public int CartId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => Price * Quantity;
        public string ImageUrl { get; set; } = string.Empty;
    }
}

