using Service.Azure.ServiceBus;
using Xunit.Abstractions;

namespace Tests.ServiceBus
{
    public class ServiceBusQueueTests(IServiceBusService serviceBusService, ITestOutputHelper output)
    {
        private readonly IServiceBusService serviceBusService = serviceBusService;
        private readonly ITestOutputHelper output = output;
        private const string QueueName = "my-queue"; // Use a queue from your appsettings.json

        public class TestMessage
        {
            public string Content { get; set; } = "Hello from ServiceBus test!";
            public int Number { get; set; } = 42;
        }

        [Fact]
        public async Task SendPeekReceiveQueueMessage_Workflow()
        {
            // Send a message
            var message = new TestMessage { Content = "Integration test message", Number = 123 };
            await serviceBusService.SendQueueMessage(QueueName, message);
            output.WriteLine($"Sent message to queue: {QueueName}");

            // Peek the message
            var peeked = await serviceBusService.PeekQueueMessages<TestMessage>(QueueName, 1);
            output.WriteLine($"Peeked message: {peeked.FirstOrDefault()?.Content}");
            Assert.NotEmpty(peeked);
            Assert.Equal(message.Content, peeked.First().Content);

            // Get message count
            var count = await serviceBusService.GetQueueMessageCount(QueueName);
            output.WriteLine($"Queue message count: {count}");
            Assert.True(count > 0);

            // Receive the message
            var received = await serviceBusService.ReceiveQueueMessages<TestMessage>(QueueName, 1);
            output.WriteLine($"Received message: {received.FirstOrDefault()?.Content}");
            Assert.NotEmpty(received);
            Assert.Equal(message.Content, received.First().Content);
        }
    }
}
