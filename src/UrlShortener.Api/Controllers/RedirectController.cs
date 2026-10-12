using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Services;
using UrlShortener.Domain.Events;
using UrlShortener.Domain.Interfaces;

namespace UrlShortener.Api.Controllers;

/// <summary>Public redirect endpoint: <c>GET /{code}</c> → 302 to the target.</summary>
[ApiController]
public class RedirectController : ControllerBase
{
    private readonly IUrlService _service;
    private readonly IEventPublisher _publisher;
    private readonly TimeProvider _timeProvider;

    public RedirectController(IUrlService service, IEventPublisher publisher, TimeProvider timeProvider)
    {
        _service = service;
        _publisher = publisher;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Resolves a code and issues a 302 redirect. 302 (not 301) is deliberate so every hit reaches
    /// the server and can be counted. A click event is published to the analytics pipeline off the
    /// hot path; expired links return 410 Gone, unknown codes return 404.
    /// </summary>
    [HttpGet("/{code}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> RedirectToTarget(string code, CancellationToken ct)
    {
        var result = await _service.ResolveAsync(code, ct);
        if (result.Status != ResolveStatus.Found)
            return result.Status == ResolveStatus.Expired
                ? StatusCode(StatusCodes.Status410Gone)
                : NotFound();

        await PublishClickAsync(result.Url!.Code, ct);
        return Redirect(result.Url!.TargetUrl);
    }

    private async Task PublishClickAsync(string code, CancellationToken ct)
    {
        var referrer = Request.Headers.Referer.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        var evt = new UrlClickedEvent(
            code,
            _timeProvider.GetUtcNow(),
            string.IsNullOrWhiteSpace(referrer) ? null : referrer,
            string.IsNullOrWhiteSpace(userAgent) ? null : userAgent);

        await _publisher.PublishAsync(evt, ct);
    }
}
