using Microsoft.AspNetCore.Mvc;
using RabbitMqClassicToQuorum.Api.Models;
using RabbitMqClassicToQuorum.Api.Services;

namespace RabbitMqClassicToQuorum.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueueController : ControllerBase
    {
        private readonly QueueService _queueService;

        public QueueController(QueueService queueService)
        {
            _queueService = queueService;
        }

        [HttpPost("ClassicToQuorum")]
        public async Task<IActionResult> ClassicToQuorum(ClassicToQuorumRequest request)
        {
            await _queueService.MigrateFromClassicToQuorumAsync(request.VirtualHost);
            return Ok();
        }
    }
}
