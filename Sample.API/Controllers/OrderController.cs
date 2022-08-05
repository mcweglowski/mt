using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Sample.Contracts;

namespace Sample.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IRequestClient<SubmitOrder> _submitOrderRequestClient;

        public OrderController(ILogger<OrderController> logger, IRequestClient<SubmitOrder> submitOrderRequestClient)
        {
            _logger = logger;
            _submitOrderRequestClient = submitOrderRequestClient;
        }

        [HttpPost()]
        public async Task<IActionResult> Post(Guid id, string customerNumber)
        {
            _logger.LogDebug($"Submit order: {id}");
            var (accepted, rejected) = await _submitOrderRequestClient.GetResponse<OrderSubmissionAccepted, OrderSubmissionRejected>(new
            {
                OrderId = id,
                Timestamp = InVar.Timestamp,
                CustomerNumber = customerNumber
            });

            if (accepted.IsCompletedSuccessfully)
            {
                _logger.LogDebug($"Order: {id} accepted");

                var response = await accepted;
                return Accepted(response);
            }
            else
            {
                _logger.LogDebug($"Order: {id} rejected");

                var response = await rejected;

                return BadRequest(response.Message);
            }
        }
    }
}