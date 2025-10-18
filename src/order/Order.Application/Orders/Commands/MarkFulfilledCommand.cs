using MediatR;
using FluentValidation;

public record MarkFulfilledCommand(Guid OrderId, DateTimeOffset FulfilledAtUtc) : IRequest;

public class MarkFulfilledCommandValidator : AbstractValidator<MarkFulfilledCommand>
{
    public MarkFulfilledCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.FulfilledAtUtc).NotEmpty();
    }
}