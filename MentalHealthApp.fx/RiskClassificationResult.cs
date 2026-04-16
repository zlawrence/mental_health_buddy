namespace MentalHealthApp.Fx;

public class RiskClassificationResult
{
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Normal;
    public double Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
}
