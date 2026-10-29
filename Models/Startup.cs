namespace InvestCase.Models;

public class Startup
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Stage { get; set; }
    public string? Industry { get; set; }
    public string? InvestmentNeededRange { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Location { get; set; }
    public DateTime? FoundedAt { get; set; }
    public int ProfileViews30Days { get; set; }
    public User User { get; set; } = null!;
}
