namespace Sample.Contracts;

public interface OrderSubmitted
{
    public Guid OrderId { get; }
    DateTime Timestamp { get; }
    string CustomerNumber { get; }
}
