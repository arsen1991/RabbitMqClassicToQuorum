using CommandLine;

namespace RabbitMqClassicToQueueMigrator
{
    public class Options
    {
        [Option('h', "host", Required = true, HelpText = "RabbitMQ host url")]
        public string HostUrl { get; set; }

        [Option('u', "username", Required = true, HelpText = "RabbitMQ username")]
        public string UserName { get; set; }

        [Option('p', "password", Required = true, HelpText = "RabbitMQ password")]
        public string Password { get; set; }

        [Option('v', "virtual-host", Required = false, Default = "/", HelpText = "RabbitMQ virtual host")]
        public string VirtualHost { get; set; }

        [Option('k', "keep-temporary-queues", Required = false, Default = false, HelpText = "Keep temporary queues")]
        public bool KeepTemporaryQueues { get; set; }
    }
}
