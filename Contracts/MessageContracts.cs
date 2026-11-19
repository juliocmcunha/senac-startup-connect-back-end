using System.ComponentModel.DataAnnotations;

namespace InvestCase.API.Contracts;

public record SendMessageRequest(
    [Required] Guid SenderId,
    [Required] Guid ReceiverId,
    [Required, StringLength(2000, MinimumLength = 1)] string Content);
