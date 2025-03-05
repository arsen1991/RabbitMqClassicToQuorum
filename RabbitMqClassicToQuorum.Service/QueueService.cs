using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMqClassicToQuorum.Api.Configuration;

namespace RabbitMqClassicToQuorum.Api.Services
{
    public class QueueService
    {
        private readonly RabbitMqClientConfiguration _rabbitMqClientConfiguration;
        private readonly ILogger<QueueService>? _logger;

        public QueueService(IOptions<RabbitMqClientConfiguration> rabbitMqClientConfiguration, ILogger<QueueService>? logger)
        {
            _rabbitMqClientConfiguration = rabbitMqClientConfiguration.Value;
            _logger = logger;
        }

        public QueueService(string hostUrl, string userName, string password, ILogger<QueueService>? logger)
        {
            _rabbitMqClientConfiguration = new RabbitMqClientConfiguration
            {
                HostUrl = hostUrl,
                UserName = userName,
                Password = password,
            };

            _logger = logger;
        }

        public async Task<IEnumerable<Queue>> MigrateFromClassicToQuorumAsync(string virtualHost, IEnumerable<string>? concreteQueues = null, bool keppTemporaryQueues = false)
        {
            _logger?.LogInformation("Starting migration from classic to quorum queues for virtual host '{VirtualHost}'", virtualHost);

            var client = new ManagementClient(new Uri(_rabbitMqClientConfiguration.HostUrl), _rabbitMqClientConfiguration.UserName, _rabbitMqClientConfiguration.Password);
            var queues = await client.GetQueuesAsync(virtualHost);

            if (concreteQueues != null)
            {
                queues = queues.Where(q => concreteQueues.Contains(q.Name)).ToList();
            }

            var bindings = await client.GetBindingsAsync(virtualHost);

            Dictionary<Queue, List<Binding>> queueBindings = new();
            foreach (var queue in queues)
            {
                if (queue.Type == QueueType.Quorum)
                {
                    _logger?.LogInformation("Queue '{QueueName}' is already a quorum queue. Skipping.", queue.Name);
                    continue;
                }

                _logger?.LogInformation("Migrating queue '{QueueName}' to quorum.", queue.Name);

                queueBindings.Add(queue, bindings.Where(b => b.Destination == queue.Name).ToList());

                var queueInfo = new QueueInfo()
                {
                    Arguments = new Dictionary<string, object?>
                        {
                            { "x-queue-type", "quorum" },
                        },
                };

                var tempQueue = $"{queue.Name}_temp";
                await client.CreateQueueAsync(virtualHost, tempQueue, queueInfo);
                _logger?.LogInformation("Created temporary queue '{TempQueueName}' for queue '{QueueName}'.", tempQueue, queue.Name);

                foreach (var binding in queueBindings[queue])
                {
                    if (!string.IsNullOrEmpty(binding.Source))
                    {
                        await client.CreateQueueBindingAsync(virtualHost, binding.Source, tempQueue, new BindingInfo(binding.RoutingKey));
                    }
                }

                var oldMessages = await client.GetMessagesFromQueueAsync(virtualHost, queue.Name, new GetMessagesFromQueueInfo(1_000_000, AckMode.AckRequeueFalse));
                _logger?.LogInformation("Retrieved {MessageCount} messages from queue '{QueueName}'.", oldMessages.Count, queue.Name);

                foreach (var message in oldMessages)
                {
                    foreach (var binding in queueBindings[queue])
                    {
                        if (!string.IsNullOrEmpty(binding.Source))
                        {
                            await client.PublishAsync(virtualHost, binding.Source, new PublishInfo(binding.RoutingKey, message.Payload));
                        }
                    }
                }

                await client.DeleteQueueAsync(virtualHost, queue.Name);
                _logger?.LogInformation("Deleted old queue '{QueueName}'.", queue.Name);

                await client.CreateQueueAsync(virtualHost, queue.Name, queueInfo);
                _logger?.LogInformation("Recreated queue '{QueueName}' as a quorum queue.", queue.Name);

                foreach (var binding in queueBindings[queue])
                {
                    if (!string.IsNullOrEmpty(binding.Source))
                    {
                        await client.CreateQueueBindingAsync(virtualHost, binding.Source, queue.Name, new BindingInfo(binding.RoutingKey));
                    }
                }

                var newMessages = await client.GetMessagesFromQueueAsync(virtualHost, tempQueue, new GetMessagesFromQueueInfo(1_000_000, AckMode.AckRequeueFalse));
                _logger?.LogInformation("Retrieved {MessageCount} messages from temporary queue '{TempQueueName}'.", newMessages.Count, tempQueue);

                foreach (var message in newMessages)
                {
                    foreach (var binding in queueBindings[queue])
                    {
                        if (!string.IsNullOrEmpty(binding.Source))
                        {
                            await client.PublishAsync(virtualHost, binding.Source, new PublishInfo(binding.RoutingKey, message.Payload));
                        }
                    }
                }

                if (!keppTemporaryQueues)
                {
                    await client.DeleteQueueAsync(virtualHost, tempQueue);
                    _logger?.LogInformation("Deleted temporary queue '{TempQueueName}'.", tempQueue);
                }
            }

            _logger?.LogInformation("Completed migration from classic to quorum queues for virtual host '{VirtualHost}'", virtualHost);

            return queues;
        }
    }
}
