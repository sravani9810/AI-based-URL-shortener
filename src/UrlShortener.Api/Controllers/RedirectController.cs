using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.Services;

namespace UrlShortener.Api.Controllers;

/// <summary>Public redirect endpoint: <c>GET /{code}</c> → 302 to the target.</summary>
[ApiController]
public class RedirectController : ControllerBase
{
    private readonly IUrlService _service;

    public RedirectController(IUrlService service) => _service = service;

    /// <summary>
    /// Resolves a code and issues a 302 redirect. 302 (not 301) is deliberate so every hit
    /// reaches the server and can be counted by the analytics pipeline (added in Phase 3).
    /// </summary>
    [HttpGet("/{code}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedirectToTarget(string code, CancellationToken ct)
    {
        var dto = await _service.ResolveAsync(code, ct);
        return dto is null ? NotFound() : Redirect(dto.TargetUrl);
    }
}
