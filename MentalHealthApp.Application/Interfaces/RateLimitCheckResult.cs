namespace MentalHealthApp.Application.Services;

public class RateLimitCheckResult
{
    public bool AllowedToChat { get; set; }
    public int CurrentCount { get; set; }
    public int MaxAllowed { get; set; }
    public string Message { get; set; } = string.Empty;
}
