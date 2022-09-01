using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Sample.Contracts;

namespace Sample.API.Controllers;

[ApiController]
[Route("[controller]")]
public class CustomerController : ControllerBase
{
    private readonly ILogger<CustomerController> _logger;
    private IPublishEndpoint _publishEndpoint;

    public CustomerController(ILogger<CustomerController> logger, IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    [HttpDelete()]
    public async Task<IActionResult> Delete(Guid id, string customerNumber)
    {
        await _publishEndpoint.Publish<CustomerAccountClosed>(new 
        {
            CustomerId = id,
            CustomerNumber = customerNumber,
        });

        return Ok();
    }

}