using aspnet_backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace aspnet_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FundingCallsController : ControllerBase
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
    public IActionResult Create(FundingCall fundingCall)
    {
        return CreatedAtAction(
            nameof(GetById),
            new { id = fundingCall.Id },
            fundingCall
        );
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, FundingCall fundingCall)
    {
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return NoContent();
    }
}