using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ST10439052_CLDV_POE.Models;
using ST10439052_CLDV_POE.Configuration;
using System.Text.Json;

namespace ST10439052_CLDV_POE.Services
{
    public class AzureStorageService : IAzureStorageService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly QueueServiceClient _queueServiceClient;
        private readonly ShareServiceClient _shareServiceClient;
        private readonly ILogger<AzureStorageService> _logger;
        private static readonly object _initLock = new();
        private static bool _initialized = false;

        public AzureStorageService(IConfiguration configuration, ILogger<AzureStorageService> logger)
        {
            var connectionString =
                configuration["AzureWebJobsStorage"]
                ?? configuration.GetConnectionString("AzureStorage")
                ?? configuration["Storage:ConnectionString"]
                ?? throw new InvalidOperationException("Azure Storage connection string not found");

            _tableServiceClient = new TableServiceClient(connectionString);
            _blobServiceClient = new BlobServiceClient(connectionString);
            _queueServiceClient = new QueueServiceClient(connectionString);
            _shareServiceClient = new ShareServiceClient(connectionString);
            _logger = logger;

            // Initialize storage only once
            if (!_initialized)
            {
                lock (_initLock)
                {
                    if (!_initialized)
                    {
                        InitializeStorageAsync().GetAwaiter().GetResult();
                        _initialized = true;
                    }
                }
            }
        }
        //st10439052
        private async Task InitializeStorageAsync()
        {
            try
            {
                _logger.LogInformation("Initializing Azure Storage...");
                // Create tables
                await _tableServiceClient.CreateTableIfNotExistsAsync("Customers");
                await _tableServiceClient.CreateTableIfNotExistsAsync("Products");
                await _tableServiceClient.CreateTableIfNotExistsAsync("Orders");

                var containers = new[]
                {
                    StorageNames.ContainerUploads,
                    StorageNames.ContainerProductImages,
                    StorageNames.ContainerPaymentProofs
                };

                foreach (var containerName in containers)
                {
                    var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                    await containerClient.CreateIfNotExistsAsync();
                }

                // Create queues (including poison queues)
                var queues = new[]
                {
                    StorageNames.QueueOrders,
                    StorageNames.QueueOrderNotifications,
                    StorageNames.QueueStockUpdates,
                    StorageNames.QueueOrderNotificationsPoison,
                    StorageNames.QueueStockUpdatesPoison
                };

                foreach (var queueName in queues)
                {
                    var queueClient = _queueServiceClient.GetQueueClient(queueName);
                    await queueClient.CreateIfNotExistsAsync();
                }

                // Create file share
                var shareClient = _shareServiceClient.GetShareClient(StorageNames.ShareContracts);
                await shareClient.CreateIfNotExistsAsync();
                var directoryClient = shareClient.GetDirectoryClient(StorageNames.ShareContractsPaymentsDir);
                await directoryClient.CreateIfNotExistsAsync();

                _logger.LogInformation("Azure Storage initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Storage: {Message}", ex.Message);
                throw;
            }
        }

        #region Table Operations
        public async Task<List<T>> GetAllEntitiesAsync<T>() where T : class, ITableEntity, new()
        {
            var tableClient = _tableServiceClient.GetTableClient(GetTableName<T>());
            var entities = new List<T>();
            await foreach (var entity in tableClient.QueryAsync<T>()) entities.Add(entity);
            return entities;
        }

        public async Task<T> GetEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient(GetTableName<T>());
                var response = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404) { return null; }
        }

        public async Task<T> AddEntityAsync<T>(T entity) where T : class, ITableEntity
        {
            var tableClient = _tableServiceClient.GetTableClient(GetTableName<T>());
            await tableClient.AddEntityAsync(entity);
            return entity;
        }

        public async Task<T> UpdateEntityAsync<T>(T entity) where T : class, ITableEntity
        {
            var tableClient = _tableServiceClient.GetTableClient(GetTableName<T>());
            var etag = entity.ETag == default || string.IsNullOrEmpty(entity.ETag.ToString()) ? ETag.All : entity.ETag;
            await tableClient.UpdateEntityAsync(entity, etag, TableUpdateMode.Replace);
            return entity;
        }

        public async Task DeleteEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            var tableClient = _tableServiceClient.GetTableClient(GetTableName<T>());
            await tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        private static string GetTableName<T>() => typeof(T).Name switch
        {
            nameof(Customer) => "Customers",
            nameof(Product) => "Products",
            nameof(Order) => "Orders",
            _ => typeof(T).Name + "s"
        };
        #endregion

        #region Blob Operations
        public async Task<string> UploadImageAsync(IFormFile file, string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blobClient = containerClient.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            if (blobClient.CanGenerateSasUri)
            {
                var sasUri = blobClient.GenerateSasUri(new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddDays(7))
                {
                    BlobContainerName = containerName,
                    BlobName = fileName
                });
                return sasUri.ToString();
            }

            return blobClient.Uri.ToString();
        }

        public async Task<string> UploadFileAsync(IFormFile file, string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();
            var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{file.FileName}";
            var blobClient = containerClient.GetBlobClient(fileName);
            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);
            return fileName;
        }

        public async Task DeleteBlobAsync(string blobName, string containerName)
        {
            var blobClient = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
        #endregion

        #region Queue Operations
        public async Task SendMessageAsync(string queueName, string message)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            await queueClient.SendMessageAsync(message);
        }

        public async Task<string> ReceiveMessageAsync(string queueName)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            var response = await queueClient.ReceiveMessageAsync();
            if (response.Value != null)
            {
                await queueClient.DeleteMessageAsync(response.Value.MessageId, response.Value.PopReceipt);
                return response.Value.MessageText;
            }
            return null;
        }
        #endregion

        #region File Share Operations
        public async Task<string> UploadToFileShareAsync(IFormFile file, string shareName, string directoryName = "")
        {
            var shareClient = _shareServiceClient.GetShareClient(shareName);
            var directoryClient = string.IsNullOrEmpty(directoryName) ? shareClient.GetRootDirectoryClient() : shareClient.GetDirectoryClient(directoryName);
            await directoryClient.CreateIfNotExistsAsync();

            var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{file.FileName}";
            var fileClient = directoryClient.GetFileClient(fileName);

            using var stream = file.OpenReadStream();
            await fileClient.CreateAsync(stream.Length);
            await fileClient.UploadAsync(stream);

            return fileName;
        }

        public async Task<byte[]> DownloadFromFileShareAsync(string shareName, string fileName, string directoryName = "")
        {
            var shareClient = _shareServiceClient.GetShareClient(shareName);
            var directoryClient = string.IsNullOrEmpty(directoryName) ? shareClient.GetRootDirectoryClient() : shareClient.GetDirectoryClient(directoryName);
            var fileClient = directoryClient.GetFileClient(fileName);

            var response = await fileClient.DownloadAsync();
            using var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
        #endregion
    }
}
