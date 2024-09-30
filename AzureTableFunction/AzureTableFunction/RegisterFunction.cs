using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public static class RegisterFunction
{
    private static readonly string TableName = "CustomerProfiles"; // Change to your table name

    [Function("RegisterUser")]
    public static async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
        FunctionContext context)
    {
        var logger = context.GetLogger("RegisterFunction");
        logger.LogInformation("C# HTTP trigger function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var registrationData = JsonSerializer.Deserialize<RegisterViewModel>(requestBody);

        // Create the response object to return
        var response = req.CreateResponse();

        if (registrationData == null || !IsValidEmail(registrationData.Email) || string.IsNullOrWhiteSpace(registrationData.Password))
        {
            response.StatusCode = HttpStatusCode.BadRequest; // Set the status code
            await response.WriteStringAsync("Invalid email or password."); // Asynchronous response writing
            return response; // Return the response
        }

        // Hash the password before storing 
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registrationData.Password);

        var profile = new CustomerProfile
        {
            PartitionKey = "Customer",
            RowKey = registrationData.Email, // Use email as RowKey for uniqueness
            Email = registrationData.Email,
            PasswordHash = passwordHash
        };

        var tableClient = new TableClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), TableName);
        await tableClient.CreateIfNotExistsAsync(); // Ensure the table exists
        await tableClient.UpsertEntityAsync(profile); // Add or update the user profile

        response.StatusCode = HttpStatusCode.Created; // Set the status code for success
        await response.WriteStringAsync("User registered successfully."); // Asynchronous response writing

        return response; // Return the response
    }

    private static bool IsValidEmail(string email)
    {
        // Simple email validation
        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        return emailRegex.IsMatch(email);
    }
}

// Define the RegisterViewModel and CustomerProfile classes as per your existing models
public class RegisterViewModel
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class CustomerProfile : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }

    // Implementing ITableEntity interface requires these properties
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
