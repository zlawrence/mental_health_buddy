namespace MentalHealthApp.Fx;

public class ChatRequest
{
    public string UserId { get; set; } = default!;
    public string Message { get; set; } = default!;
}

public class ChatResponse
{
    public string Message { get; set; } = default!;
    public RiskLevel RiskLevel { get; set; }
}
