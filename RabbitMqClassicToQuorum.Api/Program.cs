using Microsoft.Extensions.Options;
using RabbitMqClassicToQuorum.Api.Configuration;
using RabbitMqClassicToQuorum.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<RabbitMqClientConfiguration>(builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddScoped<QueueService>(x => new QueueService(x.GetService<IOptions<RabbitMqClientConfiguration>>(), x.GetService<ILogger<QueueService>>()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
