using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using Order.Domain.Entities;

namespace SmartShop.Domain.Tests;

public class OrderCreationTests
{
    [Fact]
    public void CreateOrder_Should_Set_InitialState_And_Lines_With_Totals()
    {
        // arrange
        var customerId = Guid.NewGuid();
        var lines = new[]
        {
            (productId: Guid.NewGuid(), quantity: 2, unitPrice: 10.50m),
            (productId: Guid.NewGuid(), quantity: 1, unitPrice: 99.99m)
        };

        // act
        var order = Order.Domain.Entities.Order.Create(customerId, lines);

        // assert
        order.Should().NotBeNull();
        order.OrderId.Should().NotBeEmpty();
        order.CustomerId.Should().Be(customerId);
        order.Status.Should().Be(OrderStatus.Created);
        order.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);

        order.Lines.Should().HaveCount(2);
        var total = order.Lines.Sum(l => l.Quantity * l.UnitPrice);
        total.Should().Be(2 * 10.50m + 1 * 99.99m);
    }
}