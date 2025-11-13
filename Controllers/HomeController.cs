using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST10439052_CLDV_POE.Models;
using ST10439052_CLDV_POE.Models.ViewModels;
using ST10439052_CLDV_POE.Services;
using System.Security.Claims;

namespace ST10439052_CLDV_POE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAzureStorageService _storageService;
        private readonly ICartService _cartService;

        public HomeController(ILogger<HomeController> logger, IAzureStorageService storageService, ICartService cartService)
        {
            _logger = logger;
            _storageService = storageService;
            _cartService = cartService;
        }

        public async Task<IActionResult> Index()
        {
            // If user is authenticated, redirect to appropriate dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst("Role")?.Value;
                if (role == "Admin")
                {
                    return RedirectToAction("AdminDashboard");
                }
                else if (role == "Customer")
                {
                    return RedirectToAction("CustomerDashboard");
                }
            }

            // Public home page
            var products = await _storageService.GetAllEntitiesAsync<Product>();
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            var orders = await _storageService.GetAllEntitiesAsync<Order>();
            var viewModel = new HomeViewModel
            {
                FeaturedProducts = products.Take(5).ToList(),
                ProductCount = products.Count,
                CustomerCount = customers.Count,
                OrderCount = orders.Count
            };
            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> AdminDashboard()
        {
            var role = User.FindFirst("Role")?.Value;
            if (role != "Admin")
            {
                return RedirectToAction("AccessDenied", "Login");
            }

            var products = await _storageService.GetAllEntitiesAsync<Product>();
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            var orders = await _storageService.GetAllEntitiesAsync<Order>();
            
            ViewBag.ProductCount = products.Count;
            ViewBag.CustomerCount = customers.Count;
            ViewBag.OrderCount = orders.Count;
            ViewBag.PendingOrders = orders.Count(o => o.Status == "Submitted" || o.Status == "Processing");
            ViewBag.ProcessedOrders = orders.Count(o => o.Status == "PROCESSED");
            ViewBag.PendingOrdersList = orders
                .Where(o => o.Status == "Submitted" || o.Status == "Processing")
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToList();

            return View();
        }

        [Authorize]
        public async Task<IActionResult> CustomerDashboard()
        {
            var username = User.Identity?.Name ?? string.Empty;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Login");
            }

            var allOrders = await _storageService.GetAllEntitiesAsync<Order>();
            var userOrders = allOrders
                .Where(o => o.Username == username || o.CustomerId == username)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            var cartItemCount = await _cartService.GetCartItemCountAsync(username);

            ViewBag.OrderCount = userOrders.Count;
            ViewBag.RecentOrders = userOrders.Take(5).ToList();
            ViewBag.CartItemCount = cartItemCount;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> InitializeStorage()
        {
            try
            {
                // Force re-initialization of storage
                await _storageService.GetAllEntitiesAsync<Customer>(); // This will trigger initialization
                TempData["Success"] = "Azure Storage initialized successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to initialize storage: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
