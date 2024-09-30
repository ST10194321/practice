using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System.IO;
using System.Text.Json;
using Azure.Storage.Blobs.Models;

public static class QueueTriggerfFunction
{
    [Function("QueueTriggerFunction")]
    public static async Task Run(
        [QueueTrigger("cartqueue", Connection = "AzureWebJobsStorage")] string queueMessage,
        FunctionContext context)
    {
        var logger = context.GetLogger("QueueTriggerFunction");

        logger.LogInformation("========== STARTING FUNCTION EXECUTION ==========");
        logger.LogInformation($"Timestamp: {DateTime.UtcNow}");
        logger.LogInformation($"Queue message received: {queueMessage}");
        logger.LogInformation($"Function instance ID: {context.InvocationId}");
        logger.LogInformation($"Execution context: {context.FunctionDefinition.Name}");

        try
        {
            // Validate queue message
            logger.LogInformation("Validating queue message format...");
            ValidateQueueMessage(queueMessage);
            logger.LogInformation("Queue message validated successfully.");

            // Save the product info to Blob Storage
            logger.LogInformation("Calling SaveProductToBlobStorage...");
            await SaveProductToBlobStorage(queueMessage, logger);
            logger.LogInformation("Blob storage save operation completed successfully.");
        }
        catch (JsonException jsonEx)
        {
            logger.LogError($"JSON parsing error: {jsonEx.Message}");
            logger.LogError($"Stack Trace: {jsonEx.StackTrace}");
        }
        catch (ArgumentException argEx)
        {
            logger.LogError($"Message validation error: {argEx.Message}");
            logger.LogError($"Stack Trace: {argEx.StackTrace}");
        }
        catch (Exception ex)
        {
            logger.LogError($"General error encountered: {ex.Message}");
            logger.LogError($"Stack Trace: {ex.StackTrace}");
        }
        finally
        {
            logger.LogInformation("========== ENDING FUNCTION EXECUTION ==========");
        }
    }

    private static async Task SaveProductToBlobStorage(string productJson, ILogger logger)
    {
        logger.LogInformation("========== STARTING BLOB STORAGE OPERATION ==========");
        try
        {
            // Deserialize the product JSON
            logger.LogInformation("Deserializing product JSON...");
            var product = JsonSerializer.Deserialize<Product>(productJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true  // Ignores casing
            });

            if (product == null)
            {
                logger.LogError("Deserialization resulted in null product.");
                throw new InvalidOperationException("Deserialization of product failed.");
            }

            // Serialize the product without escaping non-ASCII characters
            var serializeOptions = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true // Optional: makes the JSON output more readable
            };

            string serializedProduct = JsonSerializer.Serialize(product, serializeOptions);

            // Log the serialized product information
            logger.LogInformation($"Serialized Product Info: {serializedProduct}");

            string blobServiceConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            logger.LogInformation($"Blob connection string: {blobServiceConnectionString}");
            BlobServiceClient blobServiceClient = new BlobServiceClient(blobServiceConnectionString);

            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("cart");

            logger.LogInformation("Checking if Blob container exists or creating it...");
            await containerClient.CreateIfNotExistsAsync();
            logger.LogInformation("Blob container check complete.");

            string fileName = $"cart_{Guid.NewGuid()}.json";
            BlobClient blobClient = containerClient.GetBlobClient(fileName);
            logger.LogInformation($"Generated blob file name: {fileName}");

            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(serializedProduct)))
            {
                logger.LogInformation("Uploading product data to blob...");
                await blobClient.UploadAsync(stream, true);
                logger.LogInformation("Blob upload completed.");
            }

            logger.LogInformation("Setting HTTP headers for blob expiration...");
            await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders
            {
                ContentDisposition = "inline",
                ContentType = "application/json",
                CacheControl = "max-age=30"
            });
            logger.LogInformation("Blob HTTP headers set successfully.");

            logger.LogInformation($"Blob saved successfully: {fileName}");
        }
        catch (JsonException blobEx)
        {
            logger.LogError($"Blob storage error: {blobEx.Message}");
            logger.LogError($"Stack Trace: {blobEx.StackTrace}");
            throw; // Re-throw for further handling or retries
        }
        catch (Exception ex)
        {
            logger.LogError($"Error saving to Blob Storage: {ex.Message}");
            logger.LogError($"Stack Trace: {ex.StackTrace}");
            throw; // Re-throw for retries
        }
        finally
        {
            logger.LogInformation("========== ENDING BLOB STORAGE OPERATION ==========");
        }
    }


    private static void ValidateQueueMessage(string message)
    {
        // Detailed message validation log
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentException("Queue message cannot be null or empty.");
        }

        try
        {
            // Check if it's a valid JSON string
            JsonDocument.Parse(message);
        }
        catch (JsonException ex)
        {
            throw new JsonException("Invalid JSON format in queue message.", ex);
        }
    }
}

// Define the Product class without ImageUrl
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
}
