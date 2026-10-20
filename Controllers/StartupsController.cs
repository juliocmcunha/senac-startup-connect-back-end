using aspnet_backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace aspnet_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StartupsController : ControllerBase
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
    public IActionResult Create(Startup startup)
    {
        return CreatedAtAction(
            nameof(GetById),
            new { id = startup.Id },
            startup
        );
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, Startup startup)
    {
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return NoContent();
    }
}