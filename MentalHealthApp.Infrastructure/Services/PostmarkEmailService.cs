using System.Net.Http.Json;
using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace MentalHealthApp.Infrastructure.Services;

public sealed class PostmarkEmailService : IThirdPartyEmailService
{
    private static readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://api.postmarkapp.com")
    };

    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly ILogger<PostmarkEmailService> _logger;
    private readonly ResiliencePipeline _pipeline;

    public PostmarkEmailService(
        string apiKey,
        string fromEmail,
        ILogger<PostmarkEmailService> logger,
        ApiResiliencePipelineProvider pipelineProvider)
    {
        _apiKey = apiKey;
        _fromEmail = fromEmail;
        _logger = logger;
        _pipeline = pipelineProvider.GetPipeline("Postmark");
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            From = _fromEmail,
            To = message.To,
            Subject = message.Subject,
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
            MessageStream = "outbound"
        };

        try
        {
            await _pipeline.ExecuteAsync(async ct =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/email")
                {
                    Content = JsonContent.Create(payload)
                };
                request.Headers.Add("X-Postmark-Server-Token", _apiKey);
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogError("Postmark rejected email to {To}: {StatusCode} — {Body}",
                        message.To, response.StatusCode, body);
                    throw new InvalidOperationException($"Email delivery failed ({response.StatusCode})");
                }

                _logger.LogInformation("Email sent to {To}: {Subject}", message.To, message.Subject);
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "All retry attempts exhausted sending email to {To}", message.To);
            throw;
        }
    }
}
