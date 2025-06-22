using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Text;
using System.Threading.Tasks;

namespace EduSyncProject.Helpers
{
    public class EventHubService
    {
        private readonly string _connectionString;
        private readonly string _hubName;

        public EventHubService(string connectionString, string hubName)
        {
            _connectionString = connectionString;
            _hubName = hubName;
        }

        public async Task SendMessageAsync(string message)
        {
            await using (var producerClient = new EventHubProducerClient(_connectionString, _hubName))
            {
                using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();
                eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(message)));
                await producerClient.SendAsync(eventBatch);
            }
        }
    }
}