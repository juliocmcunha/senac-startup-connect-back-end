using InvestCase.API.Contracts;
using InvestCase.API.Data;
using InvestCase.API.Helpers;
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
    public async Task<ActionResult<FundingCall>> Create(CreateFundingCallRequest request)
    {
        if (!await _context.Users.AnyAsync(x => x.Id == request.CreatedByUserId))
            return BadRequest(new { message = "The informed CreatedByUserId does not exist." });

        var publishedAt = DateTimeHelper.ToUtc(request.PublishedAt) ?? DateTime.UtcNow;
        var deadlineAt = DateTimeHelper.ToUtc(request.DeadlineAt);
        if (deadlineAt.HasValue && deadlineAt < publishedAt)
            return BadRequest(new { message = "DeadlineAt cannot be earlier than PublishedAt." });

        var item = new FundingCall
        {
            Id = Guid.NewGuid(), Title = request.Title.Trim(), Description = request.Description,
            Industry = request.Industry, TotalAmount = request.TotalAmount,
            PublishedAt = publishedAt, DeadlineAt = deadlineAt, CallUrl = request.CallUrl,
            CreatedByUserId = request.CreatedByUserId
        };
        _context.FundingCalls.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateFundingCallRequest input)
    {
        var item = await _context.FundingCalls.FindAsync(id);
        if (item is null) return NotFound(new { message = "Funding call not found." });

        var publishedAt = DateTimeHelper.ToUtc(input.PublishedAt) ?? item.PublishedAt;
        var deadlineAt = DateTimeHelper.ToUtc(input.DeadlineAt);
        if (deadlineAt.HasValue && deadlineAt < publishedAt)
            return BadRequest(new { message = "DeadlineAt cannot be earlier than PublishedAt." });

        item.Title = input.Title.Trim(); item.Description = input.Description; item.Industry = input.Industry;
        item.TotalAmount = input.TotalAmount; item.PublishedAt = publishedAt;
        item.DeadlineAt = deadlineAt; item.CallUrl = input.CallUrl;
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
