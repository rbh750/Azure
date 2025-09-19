using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Service.Azure.ServiceBus;

public class ServiceBusService(IOptions<ServiceBusSettings> serviceBusSettings) : IAsyncDisposable, IServiceBusService
{
    private bool canDispose = false; // Prevents the client to be disposed by a task.
    private readonly ServiceBusClient client = new(serviceBusSettings.Value.ConnectionString);
    private readonly ServiceBusAdministrationClient adminClient = new(serviceBusSettings.Value.ConnectionString);

    /// <inheritdoc />
    public async Task SendTopicMessage<T>(string topicName, T message)
        => await SendMessageAsync(topicName, message);

    public async Task<List<T>> ReceiveTopicMessages<T>(string topicName, string subscription, int maxMessages = 10)
        => await ReceiveAndCompleteMessagesAsync<T>(client.CreateReceiver(topicName, subscription), maxMessages);

    /// <inheritdoc />
    public async Task<List<T>> PeekTopicMessages<T>(string topicName, string subscription, int maxMessages = 10, long? fromSequenceNumber = null)
        => await PeekMessagesAsync<T>(client.CreateReceiver(topicName, subscription), maxMessages, fromSequenceNumber);

    /// <inheritdoc />
    public async Task SendQueueMessage<T>(string queueName, T message)
        => await SendMessageAsync(queueName, message);

    /// <inheritdoc />
    public async Task<List<T>> ReceiveQueueMessages<T>(string queueName, int maxMessages = 10)
        => await ReceiveAndCompleteMessagesAsync<T>(client.CreateReceiver(queueName), maxMessages);

    /// <inheritdoc />
    public async Task<List<T>> PeekQueueMessages<T>(string queueName, int maxMessages = 10, long? fromSequenceNumber = null)
        => await PeekMessagesAsync<T>(client.CreateReceiver(queueName), maxMessages, fromSequenceNumber);

    /// <inheritdoc />
    public async Task<long> GetQueueMessageCount(string queueName)
    {
        var props = await adminClient.GetQueueRuntimePropertiesAsync(queueName);
        return props.Value.ActiveMessageCount;
    }

    // Private helper methods to eliminate code duplication
    private async Task SendMessageAsync<T>(string entityName, T message)
    {
        var sender = client.CreateSender(entityName);
        var body = JsonSerializer.Serialize(message);
        var sbMessage = new ServiceBusMessage(body);
        await sender.SendMessageAsync(sbMessage);
    }

    private static async Task<List<T>> ReceiveAndCompleteMessagesAsync<T>(ServiceBusReceiver receiver, int maxMessages)
    {
        var messages = await receiver.ReceiveMessagesAsync(maxMessages, TimeSpan.FromSeconds(20));
        var result = new List<T>();

        foreach (var msg in messages.Reverse())
        {
            var obj = DeserializeMessage<T>(msg);
            if (obj != null)
                result.Add(obj);

            await receiver.CompleteMessageAsync(msg);
        }

        return result;
    }

    private static async Task<List<T>> PeekMessagesAsync<T>(ServiceBusReceiver receiver, int maxMessages, long? fromSequenceNumber)
    {
        var messages = fromSequenceNumber.HasValue
            ? await receiver.PeekMessagesAsync(maxMessages, fromSequenceNumber.Value)
            : await receiver.PeekMessagesAsync(maxMessages);

        var result = new List<T>();

        foreach (var msg in messages)
        {
            var obj = DeserializeMessage<T>(msg);
            if (obj != null)
                result.Add(obj);
        }

        return result;
    }

    private static T? DeserializeMessage<T>(ServiceBusReceivedMessage message) => JsonSerializer.Deserialize<T>(message.Body);

    public void EnableDisposal() => canDispose = true;

    public async ValueTask DisposeAsync()
    {
        if (canDispose)
        {
            await client.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
