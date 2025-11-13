using ST10439052_CLDV_POE.Models;
using ST10439052_CLDV_POE.Models.ViewModels;

namespace ST10439052_CLDV_POE.Services
{
    public interface ICartService
    {
        Task<bool> AddToCartAsync(string username, string productId, int quantity);
        Task<bool> RemoveFromCartAsync(int cartId, string username);
        Task<bool> UpdateQuantityAsync(int cartId, string username, int quantity);
        Task<List<CartItemViewModel>> GetCartItemsAsync(string username);
        Task<int> GetCartItemCountAsync(string username);
        Task<bool> ClearCartAsync(string username);
    }
}

