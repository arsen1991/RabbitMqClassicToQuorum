namespace RabbitMqClassicToQuorum.Api.Models
{
    public class ClassicToQuorumRequest
    {
        public required string VirtualHost { get; set; }

        public IEnumerable<string>? Queues { get; set; }
    }
}
