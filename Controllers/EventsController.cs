using InvestCase.API.Data;
using InvestCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>
/// Provides endpoints for managing events.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EventsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Retrieves all events.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetAll()
    {
        var events = await _context.Events
            .AsNoTracking()
            .Include(eventItem => eventItem.CreatedByUser)
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Retrieves an event by its unique identifier.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Event>> GetById(Guid id)
    {
        var eventItem = await _context.Events
            .AsNoTracking()
            .Include(item => item.CreatedByUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (eventItem is null)
            return NotFound(new { message = "Event not found." });

        return Ok(eventItem);
    }

    /// <summary>Creates a new event.</summary>
    [HttpPost]
    public async Task<ActionResult<Event>> Create(Event eventItem)
    {
        var creatorExists = await _context.Users
            .AnyAsync(user => user.Id == eventItem.CreatedByUserId);

        if (!creatorExists)
            return BadRequest(new { message = "The informed CreatedByUserId does not exist." });

        if (eventItem.EndsAt.HasValue && eventItem.EndsAt < eventItem.StartsAt)
            return BadRequest(new { message = "EndsAt cannot be earlier than StartsAt." });

        eventItem.Id = Guid.NewGuid();

        _context.Events.Add(eventItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = eventItem.Id }, eventItem);
    }

    /// <summary>Updates an existing event.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Event eventItem)
    {
        var existingEvent = await _context.Events.FindAsync(id);

        if (existingEvent is null)
            return NotFound(new { message = "Event not found." });

        if (eventItem.EndsAt.HasValue && eventItem.EndsAt < eventItem.StartsAt)
            return BadRequest(new { message = "EndsAt cannot be earlier than StartsAt." });

        existingEvent.Title = eventItem.Title;
        existingEvent.Description = eventItem.Description;
        existingEvent.StartsAt = eventItem.StartsAt;
        existingEvent.EndsAt = eventItem.EndsAt;
        existingEvent.Location = eventItem.Location;
        existingEvent.Type = eventItem.Type;
        existingEvent.RegistrationUrl = eventItem.RegistrationUrl;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Deletes an event.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var eventItem = await _context.Events.FindAsync(id);

        if (eventItem is null)
            return NotFound(new { message = "Event not found." });

        _context.Events.Remove(eventItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
