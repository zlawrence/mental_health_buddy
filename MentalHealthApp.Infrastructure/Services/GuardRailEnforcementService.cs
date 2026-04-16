using System.Text.RegularExpressions;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;

namespace MentalHealthApp.Infrastructure.Services;

public class GuardRailEnforcementService : IGuardRailEnforcementService
{
    public Task<string> ApplyGuardRailsAsync(string content, List<GuardRail> guardRails, CancellationToken cancellationToken = default)
    {
        var result = content;

        foreach (var guardRail in guardRails.Where(g => g.IsActive))
        {
            result = guardRail.Action switch
            {
                GuardRailAction.Remove => RemoveKeyword(result, guardRail.Keyword),
                GuardRailAction.Replace => ReplaceKeyword(result, guardRail.Keyword, guardRail.Replacement ?? "[redacted]"),
                _ => result
            };
        }

        return Task.FromResult(result);
    }

    private static string RemoveKeyword(string content, string keyword)
    {
        // Case-insensitive removal of keyword, preserving word boundaries
        return Regex.Replace(content, $@"\b{Regex.Escape(keyword)}\b", "", RegexOptions.IgnoreCase);
    }

    private static string ReplaceKeyword(string content, string keyword, string replacement)
    {
        // Case-insensitive replacement, preserving word boundaries
        return Regex.Replace(content, $@"\b{Regex.Escape(keyword)}\b", replacement, RegexOptions.IgnoreCase);
    }
}
