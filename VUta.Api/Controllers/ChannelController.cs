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
        private readonly IRequestClient<AddChannel> _addChannelClient;
        private readonly IBus _bus;

        public ChannelController(
            ILogger<ChannelController> logger,
            IRequestClient<AddChannel> addChannelClient,
            IBus bus)
        {
            _logger = logger;
            _addChannelClient = addChannelClient;
            _bus = bus;
        }

        [HttpPost("{id}")]
        [ProducesResponseType(200, Type = typeof(AddChannelResult))]
        public async Task<IActionResult> AddChannelAsync(
            string id)
        {
            var result = await _addChannelClient.GetResponse<AddChannelResult>(new(id));
            return Ok(result.Message);
        }

        [HttpPost]
        public async Task<IActionResult> AddChannelsAsync(
            [FromBody] string[] ids)
        {
            await _bus.PublishBatch(ids.Select(id => new AddChannel(id)));
            return Accepted();
        }
    }
}