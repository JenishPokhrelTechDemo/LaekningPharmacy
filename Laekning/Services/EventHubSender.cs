using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Laekning.Services
{
    // Service for sending events to Azure Event Hub
    public class EventHubSender
    {
        private readonly EventHubProducerClient _producerClient; // Client for producing events to Event Hub

        // Constructor: initializes the Event Hub client using secrets from Azure Key Vault
        public EventHubSender(IConfiguration config)
        {
            // Get Key Vault URI from configuration
            string vaultUri = config["AzureKeyVault:KeyVaultUrl"];
            
            // Create a Key Vault client using DefaultAzureCredential (supports managed identity)
            var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());

            // Retrieve Event Hub connection string and hub name from Key Vault
            KeyVaultSecret secretEventHubConnectionString = client.GetSecret("EventHubConnectionString");
            KeyVaultSecret secretEventHubName =  client.GetSecret("EventHubName");

            // Initialize Event Hub producer client with retrieved secrets
            var connectionString = secretEventHubConnectionString.Value;
            var hubName = secretEventHubName.Value;
            _producerClient = new EventHubProducerClient(connectionString, hubName);
        }

        // Sends a payload as a JSON message to Event Hub
        public async Task SendAsync(object payload)
        {
            // Serialize the payload object to JSON string
            string json = JsonSerializer.Serialize(payload);

            // Create a batch for sending events
            using EventDataBatch batch = await _producerClient.CreateBatchAsync();

            // Add the serialized JSON message to the batch
            batch.TryAdd(new EventData(Encoding.UTF8.GetBytes(json)));

            // Send the batch to Event Hub
            await _producerClient.SendAsync(batch);
        }
    }
}
