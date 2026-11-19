using InvestCase.API.Contracts;
using InvestCase.API.Data;
using InvestCase.API.Helpers;
using InvestCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>Provides event CRUD endpoints.</summary>
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _context;
    public EventsController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetAll()
        => Ok(await _context.Events.AsNoTracking().OrderBy(x => x.StartsAt).ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Event>> GetById(Guid id)
    {
        var item = await _context.Events.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? NotFound(new { message = "Event not found." }) : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Event>> Create(CreateEventRequest request)
    {
        if (!await _context.Users.AnyAsync(x => x.Id == request.CreatedByUserId))
            return BadRequest(new { message = "The informed CreatedByUserId does not exist." });
        if (request.EndsAt.HasValue && request.EndsAt < request.StartsAt)
            return BadRequest(new { message = "EndsAt cannot be earlier than StartsAt." });

        var item = new Event
        {
            Id = Guid.NewGuid(), Title = request.Title.Trim(), Description = request.Description,
            StartsAt = DateTimeHelper.ToUtc(request.StartsAt), EndsAt = DateTimeHelper.ToUtc(request.EndsAt),
            Location = request.Location, Type = request.Type, RegistrationUrl = request.RegistrationUrl,
            CreatedByUserId = request.CreatedByUserId
        };
        _context.Events.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateEventRequest input)
    {
        var item = await _context.Events.FindAsync(id);
        if (item is null) return NotFound(new { message = "Event not found." });
        if (input.EndsAt.HasValue && input.EndsAt < input.StartsAt)
            return BadRequest(new { message = "EndsAt cannot be earlier than StartsAt." });

        item.Title = input.Title.Trim(); item.Description = input.Description;
        item.StartsAt = DateTimeHelper.ToUtc(input.StartsAt); item.EndsAt = DateTimeHelper.ToUtc(input.EndsAt);
        item.Location = input.Location; item.Type = input.Type; item.RegistrationUrl = input.RegistrationUrl;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _context.Events.FindAsync(id);
        if (item is null) return NotFound(new { message = "Event not found." });
        _context.Events.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
