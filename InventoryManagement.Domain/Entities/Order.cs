using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Exceptions;

namespace InventoryManagement.Domain.Entities;

/// <summary>Outgoing sales / fulfilment order. Aggregate root for <see cref="OrderItem"/>.</summary>
public sealed class Order : AuditableEntity
{
    private readonly List<OrderItem> _items = [];

    private Order() { }

    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    /// <summary>Warehouse the order is fulfilled from.</summary>
    public Guid WarehouseId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public DateTimeOffset OrderDate { get; private set; }
    public string? Notes { get; private set; }

    public Customer Customer { get; private set; } = null!;
    public Warehouse Warehouse { get; private set; } = null!;
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal TotalAmount => _items.Sum(i => i.LineTotal);

    public static Order Create(string orderNumber, Guid customerId, Guid warehouseId, DateTimeOffset now, string? notes) => new()
    {
        OrderNumber = Guard.NotBlank(orderNumber, nameof(OrderNumber), 50),
        CustomerId = Guard.NotEmpty(customerId, nameof(CustomerId)),
        WarehouseId = Guard.NotEmpty(warehouseId, nameof(WarehouseId)),
        OrderDate = now,
        Notes = Guard.Optional(notes, nameof(Notes), 500)
    };

    public void AddItem(Guid productId, int quantity, decimal unitPrice)
    {
        EnsureDraft();
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null) existing.IncreaseQuantity(quantity);
        else _items.Add(OrderItem.Create(Id, productId, quantity, unitPrice));
    }

    public void Confirm()
    {
        EnsureDraft();
        if (_items.Count == 0) throw new DomainException("An order needs at least one item.");
        Status = OrderStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Fulfilled or OrderStatus.Cancelled)
            throw new DomainException($"A {Status} order cannot be cancelled.");
        Status = OrderStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Only draft orders can be modified.");
    }
}

public sealed class OrderItem : BaseEntity
{
    private OrderItem() { }

    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => Quantity * UnitPrice;

    public Product Product { get; private set; } = null!;

    internal static OrderItem Create(Guid orderId, Guid productId, int quantity, decimal unitPrice) => new()
    {
        OrderId = orderId,
        ProductId = Guard.NotEmpty(productId, nameof(productId)),
        Quantity = Guard.Positive(quantity, nameof(quantity)),
        UnitPrice = Guard.NotNegative(unitPrice, nameof(unitPrice))
    };

    internal void IncreaseQuantity(int quantity) => Quantity += Guard.Positive(quantity, nameof(quantity));
}
