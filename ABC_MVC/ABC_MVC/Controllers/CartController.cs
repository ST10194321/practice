using ABC_MVC.Models;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;

namespace ABC_MVC.Controllers
{
    public class CartController : Controller
    {
        private readonly string _queueConnectionString = "DefaultEndpointsProtocol=https;AccountName=st10275496;AccountKey=CpfBmfw/u2CiDAGJGrNOYWedlAYXqrYgH2D+9lPjyacwFuTX+ZR7gv3DugtodgImsQQ2MbypK40f+AStDs84jQ==;EndpointSuffix=core.windows.net";
        private readonly string _queueName = "cartqueue"; // Queue name

        private readonly string _blobServiceConnectionString = "DefaultEndpointsProtocol=https;AccountName=st10275496;AccountKey=CpfBmfw/u2CiDAGJGrNOYWedlAYXqrYgH2D+9lPjyacwFuTX+ZR7gv3DugtodgImsQQ2MbypK40f+AStDs84jQ==;EndpointSuffix=core.windows.net"; // Use your blob connection string

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<Product> cartProducts = await LoadCartProductsFromBlobAsync();
            return View(cartProducts);  // Return the list of products to the view
        }

        private async Task<List<Product>> LoadCartProductsFromBlobAsync()
        {
            var products = new List<Product>();
            var blobServiceClient = new BlobServiceClient(_blobServiceConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient("cart");

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
                            products.Add(product);
                        }
                    }
                }
            }
            return products;
        }
    }
}
