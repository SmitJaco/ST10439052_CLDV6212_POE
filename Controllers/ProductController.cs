using Microsoft.AspNetCore.Mvc;
using ST10439052_CLDV_POE.Models;
using ST10439052_CLDV_POE.Services;
using ST10439052_CLDV_POE.Configuration;

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
            _logger.LogInformation("Create method called with product: {ProductName}, Price: {Price}", product.ProductName, product.Price);

            if (ModelState.IsValid)
            {
                try
                {
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Price must be greater than $0.00");
                        return View(product);
                    }

                    // Upload image if provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imageFile, StorageNames.ContainerProductImages);
                        product.ImageUrl = imageUrl;
                        _logger.LogInformation("Image uploaded successfully: {ImageUrl}", imageUrl);
                    }

                    // Set the updated timestamp
                    product.UpdatedAt = DateTimeOffset.UtcNow;

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
            _logger.LogInformation("Edit method called with ProductName: {ProductName}, Price: {Price}, RowKey: {RowKey}",
                product.ProductName, product.Price, product.RowKey);

            if (ModelState.IsValid)
            {
                try
                {
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Price must be greater than $0.00");
                        return View(product);
                    }

                    // Get the original product to preserve ETag
                    var originalProduct = await _storageService.GetEntityAsync<Product>("Product", product.RowKey);
                    if (originalProduct == null)
                    {
                        return NotFound();
                    }

                    // Update properties but keep the original ETag
                    originalProduct.ProductName = product.ProductName;
                    originalProduct.Description = product.Description;
                    originalProduct.Price = product.Price;
                    originalProduct.StockAvailable = product.StockAvailable;

                    // Upload new image if provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imageFile, StorageNames.ContainerProductImages);
                        originalProduct.ImageUrl = imageUrl;
                        _logger.LogInformation("Image uploaded: {ImageUrl}", imageUrl);
                    }

                    // Update the timestamp
                    originalProduct.UpdatedAt = DateTimeOffset.UtcNow;

                    await _storageService.UpdateEntityAsync(originalProduct);
                    TempData["Success"] = $"Product updated successfully! Price: {originalProduct.Price:C}";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
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
