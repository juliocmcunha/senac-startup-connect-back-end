using InvestCase.API.Data;
using InvestCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>Provides funding call CRUD endpoints.</summary>
[ApiController]
[Route("api/[controller]")]
public class FundingCallsController : ControllerBase
{
    private readonly AppDbContext _context;
    public FundingCallsController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FundingCall>>> GetAll()
        => Ok(await _context.FundingCalls.AsNoTracking().OrderBy(x => x.DeadlineAt).ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FundingCall>> GetById(Guid id)
    {
        var item = await _context.FundingCalls.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? NotFound(new { message = "Funding call not found." }) : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<FundingCall>> Create(FundingCall item)
    {
        if (!await _context.Users.AnyAsync(x => x.Id == item.CreatedByUserId))
            return BadRequest(new { message = "The informed CreatedByUserId does not exist." });
        item.Id = Guid.NewGuid();
        if (item.PublishedAt == default) item.PublishedAt = DateTime.UtcNow;
        _context.FundingCalls.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, FundingCall input)
    {
        var item = await _context.FundingCalls.FindAsync(id);
        if (item is null) return NotFound(new { message = "Funding call not found." });
        item.Title = input.Title; item.Description = input.Description; item.Industry = input.Industry;
        item.TotalAmount = input.TotalAmount; item.PublishedAt = input.PublishedAt;
        item.DeadlineAt = input.DeadlineAt; item.CallUrl = input.CallUrl;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _context.FundingCalls.FindAsync(id);
        if (item is null) return NotFound(new { message = "Funding call not found." });
        _context.FundingCalls.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
