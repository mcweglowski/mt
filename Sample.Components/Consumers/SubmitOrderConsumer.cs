using MassTransit;
using Microsoft.Extensions.Logging;
using Sample.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            await context.RespondAsync<OrderSubmissionAccepted>(new 
            { 
                context.Message.OrderId, 
                InVar.Timestamp,
                context.Message.CustomerNumber
            });
        }
    }
}
