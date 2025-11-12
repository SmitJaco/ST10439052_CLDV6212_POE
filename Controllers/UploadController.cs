using Microsoft.AspNetCore.Mvc;
using ST10439052_CLDV_POE.Models;
using ST10439052_CLDV_POE.Services;
using ST10439052_CLDV_POE.Configuration;
using Microsoft.Extensions.Logging;

namespace ST10439052_CLDV_POE.Controllers
{
    public class UploadController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IAzureStorageService storageService, ILogger<UploadController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        // GET: Upload
        public IActionResult Index()
        {
            return View(new FileUploadModel());
        }

        // POST: Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            _logger.LogInformation("UploadController.Index [POST] triggered. ModelState valid: {IsValid}", ModelState.IsValid);
            if (ModelState.IsValid)
            {
                try
                {
                    if (model.ProofOfPayment != null && model.ProofOfPayment.Length > 0)
                    {
                        _logger.LogInformation("Starting uploads for file {OriginalName} (size: {Length} bytes). OrderId: {OrderId}, Customer: {Customer}",
                            model.ProofOfPayment.FileName, model.ProofOfPayment.Length, model.OrderId, model.CustomerName);
                        // Upload to blob storage
                        var fileName = await _storageService.UploadFileAsync(model.ProofOfPayment, StorageNames.ContainerPaymentProofs);
                        _logger.LogInformation("Blob upload complete. Container: {Container}, StoredName: {StoredName}",
                            StorageNames.ContainerPaymentProofs, fileName);
                        // Also upload to file share for contracts
                        var shareFileName = await _storageService.UploadToFileShareAsync(model.ProofOfPayment, StorageNames.ShareContracts, StorageNames.ShareContractsPaymentsDir);
                        _logger.LogInformation("File share upload complete. Share: {Share}, Directory: {Directory}, StoredName: {StoredName}",
                            StorageNames.ShareContracts, StorageNames.ShareContractsPaymentsDir, shareFileName);

                        _logger.LogInformation("Upload flow completed successfully for {OriginalName}", model.ProofOfPayment.FileName);

                        TempData["Success"] = $"File uploaded successfully! File name: {fileName}";
                        return View(new FileUploadModel());
                    }
                    else
                    {
                        _logger.LogWarning("No file selected for upload.");
                        ModelState.AddModelError("ProofOfPayment", "Please select a file to upload.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file. OrderId: {OrderId}, Customer: {Customer}", model.OrderId, model.CustomerName);
                    ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
                }
            }

            return View(model);
        }
    }
}
