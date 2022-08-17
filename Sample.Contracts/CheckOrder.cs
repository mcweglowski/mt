namespace Sample.Contracts;

public interface CheckOrder
{
    Guid OrderId { get; set; }
}

public interface OrderNotFound
{
    Guid OrderId { get; set; }
}
