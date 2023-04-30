namespace VUta.Api.Controllers
{
    using MassTransit;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using VUta.Transport.Messages;

    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ChannelController : ControllerBase
    {
        private readonly ILogger<ChannelController> _logger;
        private readonly IRequestClient<AddChannel> _bus;

        public ChannelController(
            ILogger<ChannelController> logger,
            IRequestClient<AddChannel> bus)
        {
            _logger = logger;
            _bus = bus;
        }

        [HttpPost("{id}")]
        [ProducesResponseType(200, Type = typeof(AddChannelResult))]
        public async Task<IActionResult> AddChannelAsync(
            string id)
        {
            var result = await _bus.GetResponse<AddChannelResult>(new(id));
            return Ok(result.Message);
        }
    }
}