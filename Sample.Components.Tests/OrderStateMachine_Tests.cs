using Sample.Components.StateMachines;

namespace Sample.Components.Tests;

public class Submitting_an_order_Tests
{
    [Fact]
    public async Task should_create_a_state_istance()
    {
        var expectedCustomerNumber = "12345";

        var orderStateMachine = new OrderStateMachine();

        var harness = new InMemoryTestHarness();
        var saga = harness.StateMachineSaga<OrderState, OrderStateMachine>(orderStateMachine);

        await harness.Start();

        try
        {
            var orderId = NewId.NextGuid();

            await harness.Bus.Publish<OrderSubmitted>(new
            {
                OrderId = orderId,
                Timestamp = InVar.Timestamp,
                CustomerNumber = "12345",
            });

            // check if there is a saga created that matches given orderId
            Assert.True(saga.Created.Select(x => x.CorrelationId == orderId).Any());

            var instanceId = await saga.Exists(orderId, x => x.Submitted);
            Assert.NotNull(instanceId);

            var instance = saga.Sagas.Contains(instanceId.Value);
            Assert.Equal(instance.CustomerNumber, expectedCustomerNumber);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task should_respond_to_status_checks()
    {
        var expectedCustomerNumber = "12345";

        var orderStateMachine = new OrderStateMachine();

        var harness = new InMemoryTestHarness();
        var saga = harness.StateMachineSaga<OrderState, OrderStateMachine>(orderStateMachine);

        await harness.Start();

        try
        {
            var orderId = NewId.NextGuid();

            await harness.Bus.Publish<OrderSubmitted>(new
            {
                OrderId = orderId,
                Timestamp = InVar.Timestamp,
                CustomerNumber = "12345",
            });

            // check if there is a saga created that matches given orderId
            Assert.True(saga.Created.Select(x => x.CorrelationId == orderId).Any());

            var instanceId = await saga.Exists(orderId, x => x.Submitted);
            Assert.NotNull(instanceId);

            var requestClient = await harness.ConnectRequestClient<CheckOrder>();

            var response = await requestClient.GetResponse<OrderStatus>(new { OrderId = orderId});

            Assert.Equal(response.Message.State, orderStateMachine.Submitted.Name);
        }
        finally
        {
            await harness.Stop();
        }
    }
}
