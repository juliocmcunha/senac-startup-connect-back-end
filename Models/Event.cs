namespace InvestCase.Models;

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? Location { get; set; }
    public string? Type { get; set; }
    public string? RegistrationUrl { get; set; }
    public Guid CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; } = null!;
}
