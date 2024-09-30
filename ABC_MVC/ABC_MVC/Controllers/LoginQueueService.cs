using Azure.Storage.Queues;
using System.Threading.Tasks;

public class LoginQueueService
{
    private readonly QueueClient _queueClient;

    
    public LoginQueueService(string connectionString, string queueName)
    {
        _queueClient = new QueueClient(connectionString, queueName);
    }

    public async Task SendMessageAsync(string message)
    {
        await _queueClient.SendMessageAsync(message);
    }
}
