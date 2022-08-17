using GreenPipes;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using Microsoft.Extensions.Logging;
using Sample.Contracts;

namespace Sample.Components.Consumers
{
    public class SubmitOrderConsumer : IConsumer<SubmitOrder>
    {
        private readonly ILogger<SubmitOrderConsumer> _logger;

        public SubmitOrderConsumer(ILogger<SubmitOrderConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SubmitOrder> context)
        {
            _logger.LogDebug($"SubmitOrderConsumer: {context.Message.CustomerNumber}");

            if (context.Message.CustomerNumber.Contains("TEST"))
            {
                _logger.LogDebug($"Order Rejected: {context.Message.OrderId}");
                
                if (context.RequestId != null)
                    await context.RespondAsync<OrderSubmissionRejected>(new
                    {
                        OrderId = context.Message.OrderId,
                        Timestamp = InVar.Timestamp,
                        CustomerNumber = context.Message.CustomerNumber,
                        Reason = $"Test customer cannto submit orders: {context.Message.CustomerNumber}"
                    });

                return;
            }

            _logger.LogDebug($"Order Accepted: {context.Message.OrderId}");

            await context.Publish<OrderSubmitted>(new 
            {
                OrderId = context.Message.OrderId,
                Timestamp = context.Message.Timestamp,
                CustomerNumber = context.Message.CustomerNumber,
            });

            if (context.RequestId != null)
                await context.RespondAsync<OrderSubmissionAccepted>(new 
                { 
                    context.Message.OrderId, 
                    InVar.Timestamp,
                    context.Message.CustomerNumber
                });
        }
    }
}
