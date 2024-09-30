using ABC_MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ABC_MVC.Controllers
{
    public class RegisterController : Controller
    {
        private readonly HttpClient _httpClient;

        public RegisterController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");

                // Use the correct URL for your Azure Function
                var response = await _httpClient.PostAsync("http://localhost:7259/api/RegisterUser", content);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Login", "Login");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
                }
            }
            return View(model);
        }
    }
}
