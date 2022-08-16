using Automatonymous;
using GreenPipes;
using MassTransit;
using MassTransit.Definition;
using MassTransit.RedisIntegration;
using Sample.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Components.StateMachines
{
    public class OrderStateMachine : MassTransitStateMachine<OrderState>
    {
        public OrderStateMachine()
        {
            Event(() => OrderSubmitted, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => CheckOrder, x => 
                {
                    x.CorrelateById(m => m.Message.OrderId);
                    x.OnMissingInstance(m => m.ExecuteAsync(async context => 
                    {
                        if (context.RequestId.HasValue)
                        {
                            context.RespondAsync<OrderNotFound>(new{ context.Message.OrderId });
                        }
                    }));
                });

            InstanceState(x => x.CurrentState);

            Initially(
                When(OrderSubmitted)
                    .Then(context =>
                    {
                        context.Instance.SubmitDate = context.Data.Timestamp;
                        context.Instance.CustomerNumber = context.Data.CustomerNumber;
                        context.Instance.Updated = DateTime.UtcNow;
                    })
                    .TransitionTo(Submitted)
                );

            During(Submitted, 
                Ignore(OrderSubmitted));

            DuringAny(
                When(CheckOrder)
                    .RespondAsync(x => x.Init<OrderStatus>(new 
                    {
                       OrderId = x.Instance.CorrelationId,
                       State = x.Instance.CurrentState
                    }))
                );

            DuringAny(
                When(OrderSubmitted)
                    .Then(context => 
                    {
                        context.Instance.SubmitDate ??= context.Data.Timestamp;
                        context.Instance.CustomerNumber ??= context.Data.CustomerNumber;
                    })
                );
        }

        public State Submitted { get; private set; }

        public Event<OrderSubmitted> OrderSubmitted { get; private set; }
        public Event<CheckOrder> CheckOrder { get; private set; }
    }

    public class OrderState : SagaStateMachineInstance,
        IVersionedSaga
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }
        public string CustomerNumber { get; set; }
        public DateTime? SubmitDate { get; set; }
        public DateTime? Updated { get; set; }
        public int Version { get; set; }
    }

    public class OrderStsteMachnieDefinition : SagaDefinition<OrderState>
    {
        protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<OrderState> sagaConfigurator)
        {
            endpointConfigurator.UseMessageRetry(r => r.Intervals(500, 5000, 10000));
            endpointConfigurator.UseInMemoryOutbox();
        }
    }
}
