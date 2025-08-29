/*using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace Laekning.Services
{
    public class EventHubSender
    {
        private readonly EventHubProducerClient _producerClient;

        public EventHubSender(IConfiguration config)
        {
            var connectionString = "";
            var hubName = "" ;
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
*/