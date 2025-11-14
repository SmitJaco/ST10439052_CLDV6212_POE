using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ST10439052_CLDV_POE.Models;
using ST10439052_CLDV_POE.Services;
using ST10439052_CLDV_POE.Data;

namespace ST10439052_CLDV_POE.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class CustomerController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly AuthDbContext _context;

        public CustomerController(IAzureStorageService storageService, AuthDbContext context)
        {
            _storageService = storageService;
            _context = context;
        }

        // GET: Customer
        public async Task<IActionResult> Index()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            if (customers == null || customers.Count == 0)
            {
                var users = _context.Users.ToList();
                customers = users.Select(u => new Customer
                {
                    RowKey = u.Username,
                    Username = u.Username,
                    Name = string.Empty,
                    Surname = string.Empty,
                    Email = string.Empty,
                    ShippingAddress = string.Empty
                }).ToList();
            }
            return View(customers);
        }

        // GET: Customer/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var customer = await _storageService.GetEntityAsync<Customer>("Customer", id);
            if (customer == null)
            {
                var user = _context.Users.FirstOrDefault(u => u.Username == id);
                if (user == null)
                {
                    return NotFound();
                }
                customer = new Customer
                {
                    RowKey = user.Username,
                    Username = user.Username,
                    Name = string.Empty,
                    Surname = string.Empty,
                    Email = string.Empty,
                    ShippingAddress = string.Empty,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                await _storageService.AddEntityAsync(customer);
            }

            return View(customer);
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    customer.UpdatedAt = DateTimeOffset.UtcNow;
                    await _storageService.AddEntityAsync(customer);
                    TempData["Success"] = "Customer created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var customer = await _storageService.GetEntityAsync<Customer>("Customer", id);
            if (customer == null)
            {
                var all = await _storageService.GetAllEntitiesAsync<Customer>();
                customer = all.FirstOrDefault(c => c.RowKey == id || c.Username == id);
                if (customer == null)
                {
                    var user = _context.Users.FirstOrDefault(u => u.Username == id);
                    if (user == null)
                    {
                        return NotFound();
                    }
                    customer = new Customer
                    {
                        RowKey = user.Username,
                        Username = user.Username,
                        Name = string.Empty,
                        Surname = string.Empty,
                        Email = string.Empty,
                        ShippingAddress = string.Empty,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    await _storageService.AddEntityAsync(customer);
                }
            }

            return View(customer);
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    customer.UpdatedAt = DateTimeOffset.UtcNow;
                    await _storageService.UpdateEntityAsync(customer);
                    TempData["Success"] = "Customer updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        // GET: Customer/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var customer = await _storageService.GetEntityAsync<Customer>("Customer", id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customer/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Customer>("Customer", id);
                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
