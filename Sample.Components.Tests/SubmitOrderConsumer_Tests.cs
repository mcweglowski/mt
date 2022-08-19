using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Sample.Components.Consumers;
using Sample.Contracts;

namespace Sample.Components.Tests;

public class when_an_order_request_is_consumed_tests
{
    private readonly Mock<ILogger<SubmitOrderConsumer>> _logger = new Mock<ILogger<SubmitOrderConsumer>>();

    [Fact]
    public async Task should_response_with_acceptance_if_ok()
    {
        var harness = new InMemoryTestHarness();
        var consumer = harness.Consumer<SubmitOrderConsumer>(() => new SubmitOrderConsumer(_logger.Object));

        await harness.Start();

        try
        {
            var orderId = NewId.NextGuid();

            var requestClient = await harness.ConnectRequestClient<SubmitOrder>();

            var response = await requestClient.GetResponse<OrderSubmissionAccepted>(new
            {
                OrderId = orderId,
                Timestamp = InVar.Timestamp,
                CustomerNumber = "12345"
            });

            Assert.Equal(response.Message.OrderId, orderId);
            Assert.True(consumer.Consumed.Select<SubmitOrder>().Any());
            Assert.True(harness.Sent.Select<OrderSubmissionAccepted>().Any());
        }
        finally
        {
            await harness.Stop();
        }
    }


    [Fact]
    public async Task should_response_with_rejected_if_test()
    {
        var harness = new InMemoryTestHarness();
        var consumer = harness.Consumer<SubmitOrderConsumer>(() => new SubmitOrderConsumer(_logger.Object));

        await harness.Start();

        try
        {
            var orderId = NewId.NextGuid();

            var requestClient = await harness.ConnectRequestClient<SubmitOrder>();

            var response = await requestClient.GetResponse<OrderSubmissionRejected>(new
            {
                OrderId = orderId,
                Timestamp = InVar.Timestamp,
                CustomerNumber = "TEST12345"
            });

            Assert.Equal(response.Message.OrderId, orderId);
            Assert.True(consumer.Consumed.Select<SubmitOrder>().Any());
            Assert.True(harness.Sent.Select<OrderSubmissionRejected>().Any());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task should_consume_submit_order_commands()
    {
        var harness = new InMemoryTestHarness { TestTimeout = TimeSpan.FromSeconds(5) };
        var consumer = harness.Consumer<SubmitOrderConsumer>(() => new SubmitOrderConsumer(_logger.Object));

        await harness.Start();

        try
        {
            var orderId = NewId.NextGuid();

            await harness.InputQueueSendEndpoint.Send<SubmitOrder>(new
            {
                OrderId = orderId,
                Timestamp = InVar.Timestamp,
                CustomerNumber = "12345"
            });

            Assert.True(consumer.Consumed.Select<SubmitOrder>().Any());

            Assert.False(harness.Sent.Select<OrderSubmissionAccepted>().Any());
            Assert.False(harness.Sent.Select<OrderSubmissionRejected>().Any());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task should_not_publish_order_submitted_event_when_rejected()
    {
        var harness = new InMemoryTestHarness { TestTimeout = TimeSpan.FromSeconds(5) };
        var consumer = harness.Consumer<SubmitOrderConsumer>(() => new SubmitOrderConsumer(_logger.Object));

        await harness.Start();

        try
        {
            var orderId = NewId.NextGuid();

            await harness.InputQueueSendEndpoint.Send<SubmitOrder>(new
            {
                OrderId = orderId,
                Timestamp = InVar.Timestamp,
                CustomerNumber = "TEST12345"
            });

            Assert.True(consumer.Consumed.Select<SubmitOrder>().Any());

            Assert.False(harness.Published.Select<OrderSubmitted>().Any());
        }
        finally
        {
            await harness.Stop();
        }
    }


    [Fact]
    public async Task should_publish_order_submitted_event()
    {
        var harness = new InMemoryTestHarness();
        var consumer = harness.Consumer<SubmitOrderConsumer>(() => new SubmitOrderConsumer(_logger.Object));

        await harness.Start();

        try
        {
            var orderId = NewId.NextGuid();

            await harness.InputQueueSendEndpoint.Send<SubmitOrder>(new
            {
                OrderId = orderId,
                Timestamp = InVar.Timestamp,
                CustomerNumber = "12345"
            });

            Assert.True(harness.Published.Select<OrderSubmitted>().Any());
        }
        finally
        {
            await harness.Stop();
        }
    }
}