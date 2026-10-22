using InvestCase.Models;
using Microsoft.AspNetCore.Mvc;

namespace InvestCase.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvestorController : ControllerBase
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
    public IActionResult Create(Investor investor)
    {
        return CreatedAtAction(
            nameof(GetById),
            new { id = investor.Id },
            investor
        );
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, Investor investor)
    {
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return NoContent();
    }
}