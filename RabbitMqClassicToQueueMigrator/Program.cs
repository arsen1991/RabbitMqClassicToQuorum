using CommandLine;
using Microsoft.Extensions.Logging;
using RabbitMqClassicToQueueMigrator;
using RabbitMqClassicToQuorum.Api.Services;
using Serilog;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog();
});

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

Parser.Default.ParseArguments<Options>(args)
    .WithParsed(options =>
    {
        ILogger<QueueService> logger = new Logger<QueueService>(loggerFactory);
        var queue = new QueueService(options.HostUrl, options.UserName, options.Password, logger);

        queue.MigrateFromClassicToQuorumAsync(options.VirtualHost).GetAwaiter().GetResult();
    });