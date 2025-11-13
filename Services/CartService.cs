using Microsoft.EntityFrameworkCore;
using ST10439052_CLDV_POE.Data;
using ST10439052_CLDV_POE.Models;
using ST10439052_CLDV_POE.Models.ViewModels;
using ST10439052_CLDV_POE.Services;

namespace ST10439052_CLDV_POE.Services
{
    public class CartService : ICartService
    {
        private readonly AuthDbContext _context;
        private readonly IAzureStorageService _storageService;

        public CartService(AuthDbContext context, IAzureStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        public async Task<bool> AddToCartAsync(string username, string productId, int quantity)
        {
            try
            {
                // Check if item already exists in cart
                var existingItem = await _context.Cart
                    .FirstOrDefaultAsync(c => c.CustomerUsername == username && c.ProductId == productId);

                if (existingItem != null)
                {
                    // Update quantity
                    existingItem.Quantity += quantity;
                }
                else
                {
                    // Add new item
                    var cartItem = new Cart
                    {
                        CustomerUsername = username,
                        ProductId = productId,
                        Quantity = quantity
                    };
                    _context.Cart.Add(cartItem);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveFromCartAsync(int cartId, string username)
        {
            try
            {
                var cartItem = await _context.Cart
                    .FirstOrDefaultAsync(c => c.Id == cartId && c.CustomerUsername == username);

                if (cartItem != null)
                {
                    _context.Cart.Remove(cartItem);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateQuantityAsync(int cartId, string username, int quantity)
        {
            try
            {
                var cartItem = await _context.Cart
                    .FirstOrDefaultAsync(c => c.Id == cartId && c.CustomerUsername == username);

                if (cartItem != null)
                {
                    if (quantity <= 0)
                    {
                        _context.Cart.Remove(cartItem);
                    }
                    else
                    {
                        cartItem.Quantity = quantity;
                    }
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<CartItemViewModel>> GetCartItemsAsync(string username)
        {
            var cartItems = await _context.Cart
                .Where(c => c.CustomerUsername == username)
                .ToListAsync();

            var viewModels = new List<CartItemViewModel>();

            foreach (var item in cartItems)
            {
                // Get product details from Table Storage
                if (!string.IsNullOrEmpty(item.ProductId))
                {
                    var product = await _storageService.GetEntityAsync<Product>("Product", item.ProductId);
                    if (product != null)
                    {
                        viewModels.Add(new CartItemViewModel
                        {
                            CartId = item.Id,
                            ProductId = item.ProductId ?? string.Empty,
                            ProductName = product.ProductName,
                            Price = product.Price,
                            Quantity = item.Quantity,
                            ImageUrl = product.ImageUrl ?? string.Empty
                        });
                    }
                }
            }

            return viewModels;
        }

        public async Task<int> GetCartItemCountAsync(string username)
        {
            return await _context.Cart
                .Where(c => c.CustomerUsername == username)
                .SumAsync(c => c.Quantity);
        }

        public async Task<bool> ClearCartAsync(string username)
        {
            try
            {
                var cartItems = await _context.Cart
                    .Where(c => c.CustomerUsername == username)
                    .ToListAsync();

                _context.Cart.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

