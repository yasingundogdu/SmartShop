using System;
using FluentAssertions;
using Order.Domain.Entities;
using Xunit;

namespace SmartShop.Domain.Tests;

public class OrderLifecycleTests
{
    [Fact]
    public void MarkPaid_Then_Fulfilled_Should_Set_Timestamps_And_Status()
    {
        // arrange
        var customerId = Guid.NewGuid();
        var order = Order.Domain.Entities.Order.Create(
            customerId,
            new[] { (Guid.NewGuid(), 1, 50m) }
        );

        // act
        order.MarkPaid();
        var fulfilledAtUtc = DateTime.UtcNow;
        order.MarkFulfilled(fulfilledAtUtc);

        // assert
        order.Status.Should().Be(OrderStatus.Fulfilled);
        order.PaidAt.Should().NotBeNull();
        order.FulfilledAt.Should().NotBeNull();

        order.PaidAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        order.FulfilledAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        order.FulfilledAt!.Value.Should().BeCloseTo(fulfilledAtUtc, TimeSpan.FromSeconds(1));
    }
}