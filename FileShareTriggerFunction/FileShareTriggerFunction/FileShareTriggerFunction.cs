using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Azure.Functions.Worker.Http;
using Azure;

public static class FileShareTriggerfunction
{
    [Function("LogUserLogin")]
    public static async Task LogUserLogin(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, // Set to Anonymous
        FunctionContext context)
    {
        var logger = context.GetLogger("LogUserLogin");
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        var email = requestBody; // Assuming the request body contains just the email
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage"); // Retrieve from environment
        string fileShareName = "logreport";

        // Log the user login attempt
        await LogUserLoginAsync(connectionString, fileShareName, email, logger);

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { message = "Logged in successfully" });
    }

    private static async Task LogUserLoginAsync(string connectionString, string fileShareName, string email, ILogger logger)
    {
        try
        {
            ShareClient share = new ShareClient(connectionString, fileShareName);
            await share.CreateIfNotExistsAsync();

            string fileName = "Capybara.txt";
            ShareFileClient file = share.GetRootDirectoryClient().GetFileClient(fileName);

            if (!await file.ExistsAsync())
            {
                await file.CreateAsync(maxSize: 1024 * 1024);
            }

            string logEntry = $"{DateTime.UtcNow}: {email} logged in{Environment.NewLine}";
            byte[] data = Encoding.UTF8.GetBytes(logEntry);
            await AppendToFileAsync(file, data);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error logging user login: {ex.Message}");
        }
    }

    private static async Task AppendToFileAsync(ShareFileClient file, byte[] data)
    {
        ShareFileProperties properties = await file.GetPropertiesAsync();
        long position = properties.ContentLength;

        using (MemoryStream stream = new MemoryStream(data))
        {
            await file.UploadRangeAsync(new HttpRange(position, data.Length), stream);
        }
    }
}
