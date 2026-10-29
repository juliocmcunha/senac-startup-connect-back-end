using InvestCase.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>
/// Provides a simple health check for the API and database connection.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;

    public HealthController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Checks whether the API is running and PostgreSQL can be reached.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var databaseOnline = await _context.Database.CanConnectAsync();

        if (!databaseOnline)
        {
            return StatusCode(503, new
            {
                api = "online",
                database = "offline"
            });
        }

        return Ok(new
        {
            api = "online",
            database = "online",
            checkedAt = DateTime.UtcNow
        });
    }
}
