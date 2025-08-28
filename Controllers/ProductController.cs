using Microsoft.AspNetCore.Mvc;
using ST10439052_CLDV_POE.Models;
using ST10439052_CLDV_POE.Services;

namespace ST10439052_CLDV_POE.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IAzureStorageService storageService, ILogger<ProductController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        // GET: Product
        public async Task<IActionResult> Index()
        {
            var products = await _storageService.GetAllEntitiesAsync<Product>();
            return View(products);
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _storageService.GetEntityAsync<Product>("Product", id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Product/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            _logger.LogInformation("Create method called with product: {ProductName}", product.ProductName);
            
            // Manual price parsing to fix binding issue
            if (Request.Form.TryGetValue("PriceString", out var priceFormValue))
            {
                var priceString = priceFormValue.ToString().Trim();
                _logger.LogInformation("Raw price from form: '{PriceFormValue}'", priceString);
                
                if (decimal.TryParse(priceString, out var parsedPrice))
                {
                    product.PriceString = parsedPrice.ToString("F2");
                    _logger.LogInformation("Successfully parsed price: {Price}", parsedPrice);
                }
                else
                {
                    _logger.LogWarning("Failed to parse price: {PriceFormValue}", priceString);
                    ModelState.AddModelError("PriceString", "Please enter a valid price (e.g., 29.99)");
                }
            }
            else
            {
                _logger.LogWarning("PriceString not found in form data");
                ModelState.AddModelError("PriceString", "Price is required");
            }

            _logger.LogInformation("Final product price: {Price}", product.Price);

            if (ModelState.IsValid)
            {
                try
                {
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("PriceString", "Price must be greater than $0.00");
                        return View(product);
                    }

                    // Upload image if provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                        product.ImageUrl = imageUrl;
                        _logger.LogInformation("Image uploaded successfully: {ImageUrl}", imageUrl);
                    }

                    await _storageService.AddEntityAsync(product);
                    _logger.LogInformation("Product created successfully: {ProductName}", product.ProductName);
                    TempData["Success"] = $"Product '{product.ProductName}' created successfully with price {product.Price:C}!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }

            return View(product);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _storageService.GetEntityAsync<Product>("Product", id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            // Manual price parsing for edit too
            if (Request.Form.TryGetValue("PriceString", out var priceFormValue))
            {
                if (decimal.TryParse(priceFormValue, out var parsedPrice))
                {
                    product.PriceString = parsedPrice.ToString("F2");
                    _logger.LogInformation("Edit: Successfully parsed price: {Price}", parsedPrice);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original product to preserve ETag
                    var originalProduct = await _storageService.GetEntityAsync<Product>("Product", product.RowKey);
                    if (originalProduct == null)
                    {
                        return NotFound();
                    }

                    // Update properties but keep the original ETag
                    originalProduct.ProductName = product.ProductName;
                    originalProduct.Description = product.Description;
                    originalProduct.PriceString = product.PriceString;
                    originalProduct.StockAvailable = product.StockAvailable;

                    // Upload new image if provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                        originalProduct.ImageUrl = imageUrl;
                    }

                    await _storageService.UpdateEntityAsync(originalProduct);
                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }
            return View(product);
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _storageService.GetEntityAsync<Product>("Product", id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Product>("Product", id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
