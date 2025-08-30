using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Laekning.Services
{
    public class EventHubSender
    {
        private readonly EventHubProducerClient _producerClient;

        public EventHubSender(IConfiguration config)
        {
			string vaultUri = config["AzureKeyVault:KeyVaultUrl"];
			var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());

			KeyVaultSecret secretEventHubConnectionString = client.GetSecret("EventHubConnectionString");
			KeyVaultSecret secretEventHubName =  client.GetSecret("EventHubName");
			
            var connectionString = secretEventHubConnectionString.Value;
            var hubName = secretEventHubName.Value;
            _producerClient = new EventHubProducerClient(connectionString, hubName);
        }

        public async Task SendAsync(object payload)
        {
            string json = JsonSerializer.Serialize(payload);
            using EventDataBatch batch = await _producerClient.CreateBatchAsync();
            batch.TryAdd(new EventData(Encoding.UTF8.GetBytes(json)));
            await _producerClient.SendAsync(batch);
        }
    }
}
