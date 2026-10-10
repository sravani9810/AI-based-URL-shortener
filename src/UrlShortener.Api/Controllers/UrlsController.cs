using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Services;

namespace UrlShortener.Api.Controllers;

/// <summary>Management API for short links.</summary>
[ApiController]
[Route("api/urls")]
public class UrlsController : ControllerBase
{
    private readonly IUrlService _service;

    public UrlsController(IUrlService service) => _service = service;

    /// <summary>Creates a short link. Supply an optional <c>alias</c> and/or <c>expiresAt</c>.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShortUrlResponse>> Create(
        [FromBody] CreateShortUrlRequest request, CancellationToken ct)
    {
        var dto = await _service.CreateAsync(request, ct);
        var response = ToResponse(dto);
        return CreatedAtAction(nameof(GetByCode), new { code = dto.Code }, response);
    }

    /// <summary>Returns metadata for a short link (does not redirect).</summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<ActionResult<ShortUrlResponse>> GetByCode(string code, CancellationToken ct)
    {
        var result = await _service.ResolveAsync(code, ct);
        return result.Status switch
        {
            ResolveStatus.Found => Ok(ToResponse(result.Url!)),
            ResolveStatus.Expired => StatusCode(StatusCodes.Status410Gone),
            _ => NotFound()
        };
    }

    private ShortUrlResponse ToResponse(ShortUrlDto dto)
    {
        var shortUrl = $"{Request.Scheme}://{Request.Host}/{dto.Code}";
        return new ShortUrlResponse(
            dto.Code, shortUrl, dto.TargetUrl, dto.IsCustomAlias, dto.CreatedAt, dto.ExpiresAt);
    }
}
