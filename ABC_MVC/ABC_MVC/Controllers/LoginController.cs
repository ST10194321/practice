using ABC_MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Files.Shares;
using Azure;
using System.IO;

namespace ABC_MVC.Controllers
{
    public class LoginController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly ILogger<LoginController> _logger;
        private readonly HttpClient _httpClient; // HttpClient for making requests to Azure Function
        private readonly string _azureFunctionUrl = "http://localhost:7052/api/LogUserLogin"; // Update with your Azure Function URL
        private readonly string _connectionString = "DefaultEndpointsProtocol=https;AccountName=st10275496;AccountKey=CpfBmfw/u2CiDAGJGrNOYWedlAYXqrYgH2D+9lPjyacwFuTX+ZR7gv3DugtodgImsQQ2MbypK40f+AStDs84jQ==;EndpointSuffix=core.windows.net"; // Replace with your Azure File Share connection string
        private readonly string _fileShareName = "logreport"; // Replace with your file share name

        public LoginController(TableStorageService tableStorageService, ILogger<LoginController> logger, HttpClient httpClient)
        {
            _tableStorageService = tableStorageService;
            _logger = logger;
            _httpClient = httpClient;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var profile = await _tableStorageService.GetProfileAsync("Customer", model.Email);

                if (profile != null && BCrypt.Net.BCrypt.Verify(model.Password, profile.PasswordHash))
                {
                    await LogUserLoginAsync(model.Email); // Log to Azure Function
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                }
            }
            return View(model);
        }

        private async Task LogUserLoginAsync(string email)
        {
            try
            {
                // Create the JSON content for the request
                var jsonContent = JsonConvert.SerializeObject(email);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_azureFunctionUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to log user login via Azure Function: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error logging user login to Azure Function: {ex.Message}");
            }

            // Additional logging to Azure File Share
            await LogToFileShareAsync(email);
        }

        private async Task LogToFileShareAsync(string email)
        {
            try
            {
                ShareClient share = new ShareClient(_connectionString, _fileShareName);
                await share.CreateIfNotExistsAsync();

                // Use a unique file name based on the current date and time
                string fileName = $"Log_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{email.Replace("@", "_at_").Replace(".", "_dot_")}.txt";
                ShareFileClient file = share.GetRootDirectoryClient().GetFileClient(fileName);

                // Create a new file
                await file.CreateAsync(maxSize: 1024 * 1024); // 1 MiB

                // Create the log entry with the email
                string logEntry = $"{DateTime.UtcNow}: User {email} logged in{Environment.NewLine}";
                byte[] data = Encoding.UTF8.GetBytes(logEntry);

                using (MemoryStream stream = new MemoryStream(data))
                {
                    await file.UploadAsync(stream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error logging user login to file share: {ex.Message}");
            }
        }

    }
}
