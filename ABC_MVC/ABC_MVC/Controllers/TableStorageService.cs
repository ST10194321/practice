using ABC_MVC.Models;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ABC_MVC.Controllers
{
    public class TableStorageService
    {
        private readonly TableClient _tableClient;

        public TableStorageService(string connectionString, string tableName)
        {
            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient(tableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task AddOrUpdateProfileAsync(CustomerProfile profile)
        {
            await _tableClient.UpsertEntityAsync(profile);
        }

        public async Task<CustomerProfile> GetProfileAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<CustomerProfile>(partitionKey, rowKey); //retrieve the entity from the table
                return response.Value; // Return the retrieved profile
            }
            catch (RequestFailedException)
            {
                return null;
            }
        }
    }
}
