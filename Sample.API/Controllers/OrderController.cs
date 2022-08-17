using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Sample.Contracts;

namespace Sample.API.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly ILogger<OrderController> _logger;
    private readonly IRequestClient<SubmitOrder> _submitOrderRequestClient;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly IRequestClient<CheckOrder> _checkOrderRequestClient;

    public OrderController(ILogger<OrderController> logger, 
        IRequestClient<SubmitOrder> submitOrderRequestClient,
        ISendEndpointProvider sendEndpointProvider,
        IRequestClient<CheckOrder> checkOrderRequestClient)
    {
        _logger = logger;
        _submitOrderRequestClient = submitOrderRequestClient;
        _sendEndpointProvider = sendEndpointProvider;
        _checkOrderRequestClient = checkOrderRequestClient;
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid id)
    {
        var (status, notFound) = await _checkOrderRequestClient.GetResponse<OrderStatus, OrderNotFound>(new
        {
            OrderId = id
        });

        if (status.IsCompletedSuccessfully)
        {
            var response = await status;
            return Ok(response.Message);
        }

        var responseNotFound = await notFound;
        return NotFound(responseNotFound.Message);
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

    [HttpPut()]
    public async Task<IActionResult> Put(Guid id, string customerNumber)
    {
        var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("exchange:submit-order"));

        _logger.LogDebug($"Submit order: {id}");

        await endpoint.Send<SubmitOrder>(new
        {
            OrderId = id,
            Timestamp = InVar.Timestamp,
            CustomerNumber = customerNumber
        });

        return Accepted();
    }
}