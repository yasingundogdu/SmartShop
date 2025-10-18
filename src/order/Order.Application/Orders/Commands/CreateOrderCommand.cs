using MediatR;
using FluentValidation;

public record CreateOrderLineDto(Guid ProductId, int Quantity, decimal UnitPrice);
public record CreateOrderCommand(Guid CustomerId, IReadOnlyCollection<CreateOrderLineDto> Lines)
    : IRequest<Guid>;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThan(0);
        });
    }
}