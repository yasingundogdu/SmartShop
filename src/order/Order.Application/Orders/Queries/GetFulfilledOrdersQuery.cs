using FluentValidation;
using MediatR;
using Order.Application.Orders.Dtos;

namespace Order.Application.Orders.Queries;

public record GetFulfilledOrdersQuery(DateTimeOffset From, DateTimeOffset To, int Page = 1, int PageSize = 50) 
    : IRequest<PagedResult<FulfilledOrderDto>>;

public class GetFulfilledOrdersQueryValidator : AbstractValidator<GetFulfilledOrdersQuery>
{
    public GetFulfilledOrdersQueryValidator()
    {
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(500);
    }
}