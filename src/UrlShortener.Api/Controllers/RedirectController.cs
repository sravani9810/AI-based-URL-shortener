using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.Dtos;
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
    /// reaches the server and can be counted by the analytics pipeline (Phase 3).
    /// Expired links return 410 Gone; unknown codes return 404.
    /// </summary>
    [HttpGet("/{code}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> RedirectToTarget(string code, CancellationToken ct)
    {
        var result = await _service.ResolveAsync(code, ct);
        return result.Status switch
        {
            ResolveStatus.Found => Redirect(result.Url!.TargetUrl),
            ResolveStatus.Expired => StatusCode(StatusCodes.Status410Gone),
            _ => NotFound()
        };
    }
}
