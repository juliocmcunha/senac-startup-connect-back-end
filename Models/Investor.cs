namespace InvestCase.Models;

public class Investor
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? InvestmentType { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Description { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Startup> Startups { get; set; } = new List<Startup>();
}
