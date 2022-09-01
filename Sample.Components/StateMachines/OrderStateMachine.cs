using Automatonymous;
using MassTransit;
using Sample.Contracts;

namespace Sample.Components.StateMachines;

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
        Event(() => AccountClosed, x => x.CorrelateBy((saga, context) => saga.CustomerNumber == context.Message.CustomerNumber));

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
            Ignore(OrderSubmitted),
            When(AccountClosed)
                .TransitionTo(Canceled));

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
    public State Canceled { get; private set; }

    public Event<OrderSubmitted> OrderSubmitted { get; private set; }
    public Event<CheckOrder> CheckOrder { get; private set; }
    public Event<CustomerAccountClosed> AccountClosed { get; private set; }
}
