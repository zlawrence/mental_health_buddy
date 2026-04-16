using MentalHealthApp.Fx;

namespace MentalHealthApp.Application.Services;

public interface IRiskClassifier
{
    Task<RiskClassificationResult> ClassifyRiskAsync(string message, CancellationToken cancellationToken = default);
}
