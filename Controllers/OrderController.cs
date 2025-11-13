using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ST10439052_CLDV_POE.Models;
using ST10439052_CLDV_POE.Models.ViewModels;
using ST10439052_CLDV_POE.Services;
using System.Text.Json;
using ST10439052_CLDV_POE.Configuration;
using System.Security.Claims;

namespace ST10439052_CLDV_POE.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ICartService _cartService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IAzureStorageService storageService, ICartService cartService, ILogger<OrderController> logger)
        {
            _storageService = storageService;
            _cartService = cartService;
            _logger = logger;
        }

        private bool IsAdmin()
        {
            return User.HasClaim(c => c.Type == "Role" && c.Value == "Admin");
        }

        private string GetCurrentUsername()
        {
            return User.Identity?.Name ?? string.Empty;
        }

        // GET: Order
        public async Task<IActionResult> Index()
        {
            var allOrders = await _storageService.GetAllEntitiesAsync<Order>();
            
            // Filter by role: Admins see all, Customers see only their orders
            if (IsAdmin())
            {
                return View(allOrders);
            }
            else
            {
                var username = GetCurrentUsername();
                var userOrders = allOrders.Where(o => o.Username == username || o.CustomerId == username).ToList();
                return View(userOrders);
            }
        }

        // GET: Order/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Order/Create
        public async Task<IActionResult> Create()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            var products = await _storageService.GetAllEntitiesAsync<Product>();
            var viewModel = new OrderCreateViewModel
            {
                Customers = customers,
                Products = products
            };
            return View(viewModel);
        }

        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get customer and product details
                    var customer = await _storageService.GetEntityAsync<Customer>("Customer", model.CustomerId);
                    var product = await _storageService.GetEntityAsync<Product>("Product", model.ProductId);

                    if (customer == null || product == null)
                    {
                        ModelState.AddModelError("", "Invalid customer or product selected.");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Check stock availability
                    if (product.StockAvailable < model.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Create order with proper UTC DateTime handling
                    DateTime orderDateUtc;
                    try
                    {
                        // Ensure the date is in UTC format for Azure
                        if (model.OrderDate.Kind == DateTimeKind.Unspecified)
                        {
                            // Convert to UTC assuming it's in local time
                            orderDateUtc = DateTime.SpecifyKind(model.OrderDate, DateTimeKind.Utc);
                        }
                        else
                        {
                            // If it's already specified, ensure it's UTC
                            orderDateUtc = model.OrderDate.ToUniversalTime();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Fallback to current UTC time if date parsing fails
                        orderDateUtc = DateTime.UtcNow;
                        ModelState.AddModelError("", $"Date conversion issue: {ex.Message}. Using current date.");
                    }

                    // Calculate total price explicitly
                    decimal totalPrice = product.Price * model.Quantity;

                    // Debug logging
                    _logger.LogInformation("Creating order - Product Price: {Price}, Quantity: {Quantity}, Calculated Total: {Total}",
                        product.Price, model.Quantity, totalPrice);

                    var order = new Order
                    {
                        CustomerId = model.CustomerId,
                        Username = customer.Username,
                        ProductId = model.ProductId,
                        ProductName = product.ProductName,
                        OrderDate = orderDateUtc,
                        Quantity = model.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = totalPrice,
                        Status = model.Status,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };

                    // Ensure TotalPriceString is set for Azure Table Storage compatibility
                    order.TotalPriceString = totalPrice.ToString("F2");

                    _logger.LogInformation("Order object created - TotalPrice: {TotalPrice}", order.TotalPrice);

                    await _storageService.AddEntityAsync(order);

                    // Update product stock
                    product.StockAvailable -= model.Quantity;
                    await _storageService.UpdateEntityAsync(product);

                    // Send queue message for new order
                    var orderMessage = new
                    {
                        OrderId = order.OrderId,
                        CustomerId = order.CustomerId,
                        CustomerName = customer.Name + " " + customer.Surname,
                        ProductName = product.ProductName,
                        Quantity = order.Quantity,
                        TotalPrice = order.TotalPrice,
                        OrderDate = order.OrderDate,
                        Status = order.Status
                    };

                    await _storageService.SendMessageAsync(StorageNames.QueueOrderNotifications, JsonSerializer.Serialize(orderMessage));

                    // Send stock update message
                    var stockMessage = new
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        PreviousStock = product.StockAvailable + model.Quantity,
                        NewStock = product.StockAvailable,
                        UpdateBy = "Order System",
                        UpdateDate = DateTime.UtcNow
                    };

                    await _storageService.SendMessageAsync(StorageNames.QueueStockUpdates, JsonSerializer.Serialize(stockMessage));

                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                }
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // GET: Order/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
            {
                return NotFound();
            }

            // Only admins can edit orders
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Login");
            }

            ViewBag.IsAdmin = true;
            return View(order);
        }

        // POST: Order/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original order to preserve ETag and other system fields
                    var originalOrder = await _storageService.GetEntityAsync<Order>("Order", order.RowKey);
                    if (originalOrder == null)
                    {
                        return NotFound();
                    }

                    // Handle DateTime UTC conversion (same as Create method)
                    DateTime orderDateUtc;
                    try
                    {
                        // Ensure the date is in UTC format for Azure
                        if (order.OrderDate.Kind == DateTimeKind.Unspecified)
                        {
                            // Convert to UTC assuming it's in local time
                            orderDateUtc = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);
                        }
                        else
                        {
                            // If it's already specified, ensure it's UTC
                            orderDateUtc = order.OrderDate.ToUniversalTime();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Fallback to current UTC time if date parsing fails
                        orderDateUtc = DateTime.UtcNow;
                        ModelState.AddModelError("", $"Date conversion issue: {ex.Message}. Using current date.");
                        return View(order);
                    }

                    // Recalculate TotalPrice if UnitPrice or Quantity changed
                    if (order.UnitPrice != originalOrder.UnitPrice || order.Quantity != originalOrder.Quantity)
                    {
                        order.TotalPrice = order.UnitPrice * order.Quantity;
                        _logger.LogInformation("Recalculated TotalPrice: {NewTotal} (was: {OldTotal})",
                            order.TotalPrice, originalOrder.TotalPrice);
                    }

                    // Update the order properties
                    originalOrder.OrderDate = orderDateUtc;
                    originalOrder.Quantity = order.Quantity;
                    originalOrder.UnitPrice = order.UnitPrice;
                    originalOrder.TotalPrice = order.TotalPrice;
                    originalOrder.TotalPriceString = order.TotalPrice.ToString("F2");
                    originalOrder.Status = order.Status;
                    originalOrder.UpdatedAt = DateTimeOffset.UtcNow;

                    _logger.LogInformation("Updating order - Final TotalPrice: {TotalPrice}", originalOrder.TotalPrice);

                    await _storageService.UpdateEntityAsync(originalOrder);
                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                }
            }
            return View(order);
        }

        // GET: Order/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Order/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Order>("Order", id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Order/GetProductPrice
        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", productId);
                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.StockAvailable,
                        productName = product.ProductName
                    });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // POST: Order/CreateFromCart
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFromCart()
        {
            var username = GetCurrentUsername();
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Login");
            }

            try
            {
                var cartItems = await _cartService.GetCartItemsAsync(username);
                if (!cartItems.Any())
                {
                    TempData["Error"] = "Your cart is empty";
                    return RedirectToAction("Index", "Cart");
                }

                var ordersCreated = new List<string>();

                foreach (var item in cartItems)
                {
                    var product = await _storageService.GetEntityAsync<Product>("Product", item.ProductId);
                    if (product == null)
                    {
                        _logger.LogWarning("Product {ProductId} not found in cart", item.ProductId);
                        continue;
                    }

                    // Check stock
                    if (product.StockAvailable < item.Quantity)
                    {
                        TempData["Error"] = $"Insufficient stock for {product.ProductName}. Available: {product.StockAvailable}";
                        return RedirectToAction("Index", "Cart");
                    }

                    // Create order
                    var order = new Order
                    {
                        CustomerId = username, // Store username as CustomerId
                        Username = username,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        OrderDate = DateTime.UtcNow,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price,
                        TotalPrice = item.TotalPrice,
                        Status = "Submitted",
                        UpdatedAt = DateTimeOffset.UtcNow
                    };

                    order.TotalPriceString = order.TotalPrice.ToString("F2");

                    await _storageService.AddEntityAsync(order);

                    // Update product stock
                    product.StockAvailable -= item.Quantity;
                    await _storageService.UpdateEntityAsync(product);

                    // Send queue message for new order
                    var orderMessage = new
                    {
                        OrderId = order.OrderId,
                        CustomerId = order.CustomerId,
                        CustomerName = username,
                        ProductName = product.ProductName,
                        Quantity = order.Quantity,
                        TotalPrice = order.TotalPrice,
                        OrderDate = order.OrderDate,
                        Status = order.Status
                    };

                    await _storageService.SendMessageAsync(StorageNames.QueueOrderNotifications, JsonSerializer.Serialize(orderMessage));

                    ordersCreated.Add(order.OrderId);
                }

                // Clear cart
                await _cartService.ClearCartAsync(username);

                TempData["Success"] = $"Successfully created {ordersCreated.Count} order(s)!";
                return RedirectToAction("Confirmation", "Cart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order from cart");
                TempData["Error"] = $"Error creating order: {ex.Message}";
                return RedirectToAction("Index", "Cart");
            }
        }

        // GET: Order/MyOrders
        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var username = GetCurrentUsername();
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Login");
            }

            var allOrders = await _storageService.GetAllEntitiesAsync<Order>();
            var userOrders = allOrders
                .Where(o => o.Username == username || o.CustomerId == username)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(userOrders);
        }

        // GET: Order/Manage
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Manage()
        {
            var allOrders = await _storageService.GetAllEntitiesAsync<Order>();
            var orders = allOrders.OrderByDescending(o => o.OrderDate).ToList();
            
            ViewBag.IsAdmin = true;
            return View(orders);
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
            model.Products = await _storageService.GetAllEntitiesAsync<Product>();
        }
    }
}
