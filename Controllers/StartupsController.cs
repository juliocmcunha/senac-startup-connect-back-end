using InvestCase.API.Data;
using InvestCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>
/// Provides endpoints for managing startups.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StartupsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StartupsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Retrieves all startups.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Startup>>> GetAll()
    {
        var startups = await _context.Startups
            .AsNoTracking()
            .Include(startup => startup.User)
            .ToListAsync();

        return Ok(startups);
    }

    /// <summary>Retrieves a startup by its unique identifier.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Startup>> GetById(Guid id)
    {
        var startup = await _context.Startups
            .AsNoTracking()
            .Include(startup => startup.User)
            .Include(startup => startup.Investors)
            .FirstOrDefaultAsync(startup => startup.Id == id);

        if (startup is null)
            return NotFound(new { message = "Startup not found." });

        return Ok(startup);
    }

    /// <summary>Creates a new startup profile.</summary>
    [HttpPost]
    public async Task<ActionResult<Startup>> Create(Startup startup)
    {
        var userExists = await _context.Users
            .AnyAsync(user => user.Id == startup.UserId);

        if (!userExists)
            return BadRequest(new { message = "The informed UserId does not exist." });

        var profileAlreadyExists = await _context.Startups
            .AnyAsync(existingStartup => existingStartup.UserId == startup.UserId);

        if (profileAlreadyExists)
            return Conflict(new { message = "This user already has a startup profile." });

        startup.Id = Guid.NewGuid();

        _context.Startups.Add(startup);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = startup.Id }, startup);
    }

    /// <summary>Updates an existing startup.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Startup startup)
    {
        var existingStartup = await _context.Startups.FindAsync(id);

        if (existingStartup is null)
            return NotFound(new { message = "Startup not found." });

        existingStartup.Name = startup.Name;
        existingStartup.Description = startup.Description;
        existingStartup.Stage = startup.Stage;
        existingStartup.Industry = startup.Industry;
        existingStartup.WebsiteUrl = startup.WebsiteUrl;
        existingStartup.Location = startup.Location;
        existingStartup.FoundedAt = startup.FoundedAt;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Deletes a startup.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var startup = await _context.Startups.FindAsync(id);

        if (startup is null)
            return NotFound(new { message = "Startup not found." });

        _context.Startups.Remove(startup);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
