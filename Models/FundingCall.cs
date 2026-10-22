namespace InvestCase.Models;

public class FundingCall
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Industry { get; set; }
    public decimal? TotalAmount { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime? DeadlineAt { get; set; }
    public string? CallUrl { get; set; }
    public Guid CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; } = null!;
}
