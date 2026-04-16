using MentalHealthApp.Application.Services;
using System.Text.RegularExpressions;
using MentalHealthApp.Fx;

namespace MentalHealthApp.Infrastructure.Services;

public class RiskClassifier : IRiskClassifier
{
    private static readonly string[] CrisisKeywords = new[]
    {
        "suicide",
        "kill myself",
        "hurt myself",
        "cut myself",
        "overdose",
        "end it all",
        "no point living",
        "want to die",
        "going to kill",
        "harm myself",
        "self harm",
        "slash my wrists",
        "hang myself",
        "jump off",
        "poison myself",
        "ready to die",
        "life is worthless",
        "nothing to live for"
    };

    private static readonly string[] ElevatedKeywords = new[]
    {
        "hopeless",
        "isolated",
        "alone",
        "no one understands",
        "i can't do this",
        "it's too much",
        "everything is pointless",
        "i'm drowning",
        "so overwhelmed"
    };

    private static readonly string[] DistressKeywords = new[]
    {
        "anxiety",
        "anxious",
        "sad",
        "sadness",
        "panic",
        "nervous",
        "depressed",
        "stress",
        "worried",
        "fearful",
        "upset"
    };

    public Task<RiskClassificationResult> ClassifyRiskAsync(string message, CancellationToken cancellationToken = default)
    {
        var lowerMessage = message.ToLowerInvariant();

        if (ContainsKeyword(lowerMessage, CrisisKeywords))
        {
            return Task.FromResult(new RiskClassificationResult
            {
                RiskLevel = RiskLevel.Crisis,
                Confidence = 0.95,
                Reason = "Detected crisis-level risk keywords"
            });
        }

        if (ContainsKeyword(lowerMessage, ElevatedKeywords))
        {
            return Task.FromResult(new RiskClassificationResult
            {
                RiskLevel = RiskLevel.Elevated,
                Confidence = 0.80,
                Reason = "Detected elevated risk keywords"
            });
        }

        if (ContainsKeyword(lowerMessage, DistressKeywords))
        {
            return Task.FromResult(new RiskClassificationResult
            {
                RiskLevel = RiskLevel.Distress,
                Confidence = 0.70,
                Reason = "Detected distress-level keywords"
            });
        }

        if (ContainsSelfHarmContext(lowerMessage))
        {
            return Task.FromResult(new RiskClassificationResult
            {
                RiskLevel = RiskLevel.Crisis,
                Confidence = 0.85,
                Reason = "Detected crisis-level self-harm context"
            });
        }

        return Task.FromResult(new RiskClassificationResult
        {
            RiskLevel = RiskLevel.Normal,
            Confidence = 0.0,
            Reason = "No risk indicators detected"
        });
    }

    private static bool ContainsSelfHarmContext(string lowerMessage)
    {
        // Check for dangerous phrases combined with emotional distress indicators
        var dangerousPhrases = new[] { "bye", "goodbye", "last message", "final message", "it's over", "i'm done" };
        var distressKeywords = new[] { "can't", "impossible", "hopeless", "nothing works", "give up" };

        var hasDangerousPhrase = dangerousPhrases.Any(p => lowerMessage.Contains(p));
        var hasDistressKeyword = distressKeywords.Any(k => lowerMessage.Contains(k));

        return hasDangerousPhrase && hasDistressKeyword && lowerMessage.Length > 20;
    }

    private static bool ContainsKeyword(string lowerMessage, string[] keywords)
    {
        return keywords.Any(keyword => Regex.IsMatch(lowerMessage, $"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase));
    }
}
