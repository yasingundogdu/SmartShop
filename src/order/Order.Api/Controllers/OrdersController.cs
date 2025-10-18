using MediatR;
using Microsoft.AspNetCore.Mvc;
using Order.Application.Orders.Queries;

namespace Order.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet("fulfilled")]
    public async Task<IActionResult> GetFulfilled(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetFulfilledOrdersQuery(from, to, page, pageSize), ct);
        return Ok(result);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand cmd, [FromServices] IMediator mediator, CancellationToken ct)
    {
        var id = await mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetFulfilled), new { from = DateTimeOffset.UtcNow.AddYears(-1), to = DateTimeOffset.UtcNow }, id);
    }

    [HttpPatch("{id:guid}/fulfill")]
    public async Task<IActionResult> Fulfill(Guid id, [FromQuery] DateTimeOffset? fulfilledAtUtc, [FromServices] IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new MarkFulfilledCommand(id, fulfilledAtUtc ?? DateTimeOffset.UtcNow), ct);
        return StatusCode(204);
    }
}