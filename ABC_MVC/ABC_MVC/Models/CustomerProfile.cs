using Azure;
using Azure.Data.Tables;

namespace ABC_MVC.Models
{

    public class CustomerProfile : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; } //unique identifier for an entity
        public string PasswordHash { get; set; } // Password hash for the customer profile
        public string Email { get; set; }  // Email address associated with the customer profile


        public DateTimeOffset? Timestamp { get; set; }  // Timestamp indicating the last time the entity was modified
        public ETag ETag { get; set; }
    }

}
