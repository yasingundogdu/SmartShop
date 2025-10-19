using MediatR;
using Microsoft.AspNetCore.Mvc;
using Customer.Application.Customers.Queries;

namespace Customer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    public CustomersController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await _mediator.Send(new GetCustomerByIdQuery(id), ct);
        return dto is null ? NotFound() : Ok(dto);
    }
}