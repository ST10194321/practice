using ABC_MVC.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ABC_MVC.Controllers
{
    public class ProductController : Controller
    {
        private readonly string _blobServiceConnectionString = "DefaultEndpointsProtocol=https;AccountName=st10275496;AccountKey=CpfBmfw/u2CiDAGJGrNOYWedlAYXqrYgH2D+9lPjyacwFuTX+ZR7gv3DugtodgImsQQ2MbypK40f+AStDs84jQ==;EndpointSuffix=core.windows.net"; // Azure Blob Storage connection string
        private readonly string _containerName = "multimedia"; // Blob storage container name
        private readonly string _queueConnectionString = "DefaultEndpointsProtocol=https;AccountName=st10275496;AccountKey=CpfBmfw/u2CiDAGJGrNOYWedlAYXqrYgH2D+9lPjyacwFuTX+ZR7gv3DugtodgImsQQ2MbypK40f+AStDs84jQ==;EndpointSuffix=core.windows.net";   // Azure Queue Storage connection string
        private readonly string _queueName = "cartqueue"; // Queue name

        public static readonly List<Product> Products = new List<Product>();

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await LoadProductsFromBlobAsync();
            return View(Products);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                string imageUrl = await UploadImageToBlobAsync(model.ImageFile);
                var product = new Product // Create a new product instance
                {
                    Id = Products.Count + 1, 
                    Name = model.Name,
                    Price = model.Price,
                    Description = model.Description,
                    ImageUrl = imageUrl
                };

                Products.Add(product);   // Add the product to the list
                await SaveProductMetadataToBlobAsync(product);  // Save the product metadata to Azure Blob Storage

                return RedirectToAction("Index");
            }

            return View(model);
        }

        private async Task<string> UploadImageToBlobAsync(IFormFile imageFile)  // Method to upload an image to Azure Blob Storage
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            var blobServiceClient = new BlobServiceClient(_blobServiceConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            await blobContainerClient.CreateIfNotExistsAsync();

            var blobClient = blobContainerClient.GetBlobClient(imageFile.FileName);
            using (var stream = imageFile.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            return blobClient.Uri.ToString();
        }

        private async Task SaveProductMetadataToBlobAsync(Product product)
        {
            var blobServiceClient = new BlobServiceClient(_blobServiceConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            await blobContainerClient.CreateIfNotExistsAsync();

            var metadataBlobClient = blobContainerClient.GetBlobClient($"{product.Name}.json");
            var metadataJson = JsonSerializer.Serialize(product);
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(metadataJson)))
            {
                await metadataBlobClient.UploadAsync(stream, true);
            }
        }

        private async Task LoadProductsFromBlobAsync()
        {
            Products.Clear();
            var blobServiceClient = new BlobServiceClient(_blobServiceConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            await foreach (BlobItem blobItem in blobContainerClient.GetBlobsAsync())
            {
                if (blobItem.Name.EndsWith(".json"))
                {
                    var blobClient = blobContainerClient.GetBlobClient(blobItem.Name);
                    var downloadInfo = await blobClient.DownloadAsync();
                    using (var stream = new StreamReader(downloadInfo.Value.Content))
                    {
                        var json = await stream.ReadToEndAsync();
                        var product = JsonSerializer.Deserialize<Product>(json);
                        if (product != null)
                        {
                            Products.Add(product);
                        }
                    }
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId)
        {
            var product = Products.Find(p => p.Id == productId);
            if (product != null)
            {
                string productJson = JsonSerializer.Serialize(product);
                await SendMessageToQueueAsync(productJson);
            }

            return RedirectToAction("Index");
        }

        private async Task SendMessageToQueueAsync(string message)
        {
            try
            {
                QueueClient queueClient = new QueueClient(_queueConnectionString, _queueName);

                await queueClient.CreateIfNotExistsAsync();

                // Sends the message to the queue
                await queueClient.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message to queue: {ex.Message}");
            }
        }
    }
}
