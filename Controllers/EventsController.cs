using aspnet_backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace aspnet_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok();
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        return Ok();
    }

    [HttpPost]
    public IActionResult Create(Event @event)
    {
        return CreatedAtAction(
            nameof(GetById),
            new { id = @event.Id },
            @event
        );
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, Event @event)
    {
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return NoContent();
    }
}