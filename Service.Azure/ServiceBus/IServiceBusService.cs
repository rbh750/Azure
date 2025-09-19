namespace Service.Azure.ServiceBus
{
    /// <summary>
    /// Provides operations for interacting with Azure Service Bus queues and topics,
    /// including sending, receiving, and peeking at messages with automatic serialization.
    /// </summary>
    public interface IServiceBusService
    {
        /// <summary>
        /// Asynchronously disposes of Service Bus resources.
        /// Should be called when the service is no longer needed to free up connections.
        /// </summary>
        /// <returns>A ValueTask representing the asynchronous dispose operation</returns>
        ValueTask DisposeAsync();
        
        /// <summary>
        /// Enables disposal of Service Bus resources.
        /// Must be called before DisposeAsync to allow proper cleanup.
        /// </summary>
        void EnableDisposal();
        
        /// <summary>
        /// Gets the count of active messages in a Service Bus queue.
        /// This does not include dead letter messages.
        /// </summary>
        /// <param name="queueName">The name of the queue to check</param>
        /// <returns>The number of active messages in the queue</returns>
        Task<long> GetQueueMessageCount(string queueName);
        
        /// <summary>
        /// Peeks at messages in a Service Bus queue without removing them.
        /// This allows inspection of messages without consuming them.
        /// 
        /// Example usage:
        /// <code>
        /// var messages = await serviceBus.PeekQueueMessages&lt;MyMessage&gt;("my-queue", 5);
        /// foreach (var msg in messages)
        /// {
        ///     Console.WriteLine($"Message: {msg.Content}");
        /// }
        /// </code>
        /// </summary>
        /// <typeparam name="T">The type to deserialize messages to</typeparam>
        /// <param name="queueName">The name of the queue</param>
        /// <param name="maxMessages">Maximum number of messages to peek (default: 10)</param>
        /// <param name="fromSequenceNumber">Optional sequence number to start peeking from</param>
        /// <returns>List of peeked messages</returns>
        Task<List<T>> PeekQueueMessages<T>(string queueName, int maxMessages = 10, long? fromSequenceNumber = null);
        
        /// <summary>
        /// Peeks at messages in a Service Bus topic subscription without removing them.
        /// This allows inspection of messages without consuming them.
        /// </summary>
        /// <typeparam name="T">The type to deserialize messages to</typeparam>
        /// <param name="topicName">The name of the topic</param>
        /// <param name="subscription">The name of the subscription</param>
        /// <param name="maxMessages">Maximum number of messages to peek (default: 10)</param>
        /// <param name="fromSequenceNumber">Optional sequence number to start peeking from</param>
        /// <returns>List of peeked messages</returns>
        Task<List<T>> PeekTopicMessages<T>(string topicName, string subscription, int maxMessages = 10, long? fromSequenceNumber = null);
        
        /// <summary>
        /// Receives and completes messages from a Service Bus queue.
        /// Messages are automatically completed after successful processing.
        /// 
        /// Example usage:
        /// <code>
        /// var orders = await serviceBus.ReceiveQueueMessages&lt;Order&gt;("order-queue", 10);
        /// foreach (var order in orders)
        /// {
        ///     await ProcessOrder(order);
        /// }
        /// </code>
        /// </summary>
        /// <typeparam name="T">The type to deserialize messages to</typeparam>
        /// <param name="queueName">The name of the queue</param>
        /// <param name="maxMessages">Maximum number of messages to receive (default: 10)</param>
        /// <returns>List of received messages</returns>
        Task<List<T>> ReceiveQueueMessages<T>(string queueName, int maxMessages = 10);
        
        /// <summary>
        /// Receives and completes messages from a Service Bus topic subscription.
        /// Messages are automatically completed after successful processing.
        /// </summary>
        /// <typeparam name="T">The type to deserialize messages to</typeparam>
        /// <param name="topicName">The name of the topic</param>
        /// <param name="subscription">The name of the subscription</param>
        /// <param name="maxMessages">Maximum number of messages to receive (default: 10)</param>
        /// <returns>List of received messages</returns>
        Task<List<T>> ReceiveTopicMessages<T>(string topicName, string subscription, int maxMessages = 10);
        
        /// <summary>
        /// Sends a message to a Service Bus queue.
        /// The message is automatically serialized to JSON.
        /// 
        /// Example usage:
        /// <code>
        /// var order = new Order { Id = 123, Amount = 99.99m };
        /// await serviceBus.SendQueueMessage("order-queue", order);
        /// </code>
        /// </summary>
        /// <typeparam name="T">The type of message to send</typeparam>
        /// <param name="queueName">The name of the queue</param>
        /// <param name="message">The message to send</param>
        Task SendQueueMessage<T>(string queueName, T message);
        
        /// <summary>
        /// Sends a message to a Service Bus topic.
        /// The message is automatically serialized to JSON and delivered to all subscriptions.
        /// </summary>
        /// <typeparam name="T">The type of message to send</typeparam>
        /// <param name="topicName">The name of the topic</param>
        /// <param name="message">The message to send</param>
        Task SendTopicMessage<T>(string topicName, T message);
    }
}