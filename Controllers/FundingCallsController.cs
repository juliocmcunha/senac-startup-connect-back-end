using InvestCase.API.Data;
using InvestCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>
/// Provides endpoints for managing funding calls.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FundingCallsController : ControllerBase
{
    private readonly AppDbContext _context;

    public FundingCallsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Retrieves all funding calls.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FundingCall>>> GetAll()
    {
        var fundingCalls = await _context.FundingCalls
            .AsNoTracking()
            .Include(fundingCall => fundingCall.CreatedByUser)
            .ToListAsync();

        return Ok(fundingCalls);
    }

    /// <summary>Retrieves a funding call by its unique identifier.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FundingCall>> GetById(Guid id)
    {
        var fundingCall = await _context.FundingCalls
            .AsNoTracking()
            .Include(item => item.CreatedByUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (fundingCall is null)
            return NotFound(new { message = "Funding call not found." });

        return Ok(fundingCall);
    }

    /// <summary>Creates a new funding call.</summary>
    [HttpPost]
    public async Task<ActionResult<FundingCall>> Create(FundingCall fundingCall)
    {
        var creatorExists = await _context.Users
            .AnyAsync(user => user.Id == fundingCall.CreatedByUserId);

        if (!creatorExists)
            return BadRequest(new { message = "The informed CreatedByUserId does not exist." });

        if (fundingCall.PublishedAt == default)
            fundingCall.PublishedAt = DateTime.UtcNow;

        if (fundingCall.DeadlineAt.HasValue &&
            fundingCall.DeadlineAt < fundingCall.PublishedAt)
        {
            return BadRequest(new { message = "DeadlineAt cannot be earlier than PublishedAt." });
        }

        fundingCall.Id = Guid.NewGuid();

        _context.FundingCalls.Add(fundingCall);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = fundingCall.Id }, fundingCall);
    }

    /// <summary>Updates an existing funding call.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, FundingCall fundingCall)
    {
        var existingFundingCall = await _context.FundingCalls.FindAsync(id);

        if (existingFundingCall is null)
            return NotFound(new { message = "Funding call not found." });

        if (fundingCall.DeadlineAt.HasValue &&
            fundingCall.DeadlineAt < fundingCall.PublishedAt)
        {
            return BadRequest(new { message = "DeadlineAt cannot be earlier than PublishedAt." });
        }

        existingFundingCall.Title = fundingCall.Title;
        existingFundingCall.Description = fundingCall.Description;
        existingFundingCall.Industry = fundingCall.Industry;
        existingFundingCall.TotalAmount = fundingCall.TotalAmount;
        existingFundingCall.PublishedAt = fundingCall.PublishedAt;
        existingFundingCall.DeadlineAt = fundingCall.DeadlineAt;
        existingFundingCall.CallUrl = fundingCall.CallUrl;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Deletes a funding call.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var fundingCall = await _context.FundingCalls.FindAsync(id);

        if (fundingCall is null)
            return NotFound(new { message = "Funding call not found." });

        _context.FundingCalls.Remove(fundingCall);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
