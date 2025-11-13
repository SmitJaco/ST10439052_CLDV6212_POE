using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST10439052_CLDV_POE.Models.ViewModels;
using ST10439052_CLDV_POE.Services;
using System.Security.Claims;

namespace ST10439052_CLDV_POE.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService cartService, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        // GET: Cart/Index
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name ?? string.Empty;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Login");
            }

            var cartItems = await _cartService.GetCartItemsAsync(username);
            ViewBag.TotalPrice = cartItems.Sum(item => item.TotalPrice);
            
            return View(cartItems);
        }

        // POST: Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(string productId, int quantity = 1)
        {
            var username = User.Identity?.Name ?? string.Empty;
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Please login to add items to cart" });
            }

            try
            {
                var success = await _cartService.AddToCartAsync(username, productId, quantity);
                if (success)
                {
                    var itemCount = await _cartService.GetCartItemCountAsync(username);
                    return Json(new { success = true, message = "Item added to cart", itemCount });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to add item to cart" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartId, int quantity)
        {
            var username = User.Identity?.Name ?? string.Empty;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Login");
            }

            try
            {
                var success = await _cartService.UpdateQuantityAsync(cartId, username, quantity);
                if (success)
                {
                    TempData["Success"] = "Cart updated successfully";
                }
                else
                {
                    TempData["Error"] = "Failed to update cart";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity");
                TempData["Error"] = "An error occurred while updating cart";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/RemoveFromCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int cartId)
        {
            var username = User.Identity?.Name ?? string.Empty;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Login");
            }

            try
            {
                var success = await _cartService.RemoveFromCartAsync(cartId, username);
                if (success)
                {
                    TempData["Success"] = "Item removed from cart";
                }
                else
                {
                    TempData["Error"] = "Failed to remove item from cart";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                TempData["Error"] = "An error occurred while removing item";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/ClearCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            var username = User.Identity?.Name ?? string.Empty;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Login");
            }

            try
            {
                var success = await _cartService.ClearCartAsync(username);
                if (success)
                {
                    TempData["Success"] = "Cart cleared successfully";
                }
                else
                {
                    TempData["Error"] = "Failed to clear cart";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                TempData["Error"] = "An error occurred while clearing cart";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Cart/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var username = User.Identity?.Name ?? string.Empty;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Login");
            }

            var cartItems = await _cartService.GetCartItemsAsync(username);
            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.TotalPrice = cartItems.Sum(item => item.TotalPrice);
            return View(cartItems);
        }

        // POST: Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutConfirmed()
        {
            var username = User.Identity?.Name ?? string.Empty;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Login");
            }

            // This will be handled by OrderController - redirect there
            return RedirectToAction("CreateFromCart", "Order");
        }
    }
}

