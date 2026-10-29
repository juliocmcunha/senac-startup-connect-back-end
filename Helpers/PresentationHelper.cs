using InvestCase.Models;

namespace InvestCase.API.Helpers;

public static class PresentationHelper
{
    public static int CalculateMatch(Investor investor, Startup startup)
    {
        var score = 68;

        if (!string.IsNullOrWhiteSpace(investor.InterestArea) &&
            !string.IsNullOrWhiteSpace(startup.Industry) &&
            investor.InterestArea.Contains(startup.Industry, StringComparison.OrdinalIgnoreCase))
            score += 20;

        if (!string.IsNullOrWhiteSpace(investor.Location) && !string.IsNullOrWhiteSpace(startup.Location))
        {
            var investorState = LastLocationPart(investor.Location);
            var startupState = LastLocationPart(startup.Location);
            if (!string.IsNullOrWhiteSpace(investorState) &&
                investorState.Equals(startupState, StringComparison.OrdinalIgnoreCase))
                score += 7;
        }

        return Math.Clamp(score, 60, 98);
    }

    public static string MatchType(int score) => score >= 85 ? "high" : "medium";

    public static string Initials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "IC";
        if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    public static string RelativeTime(DateTime date)
    {
        var utc = date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime();
        var diff = DateTime.UtcNow - utc;
        if (diff.TotalMinutes < 1) return "agora";
        if (diff.TotalHours < 1) return $"há {(int)diff.TotalMinutes} min";
        if (diff.TotalDays < 1) return $"há {(int)diff.TotalHours}h";
        if (diff.TotalDays < 2) return "ontem";
        return $"há {(int)diff.TotalDays} dias";
    }

    private static string LastLocationPart(string value)
    {
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 1 ? parts[^1] : value.Trim();
    }
}
