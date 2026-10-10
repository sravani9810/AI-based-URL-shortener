namespace UrlShortener.Api.Controllers;

/// <summary>Named rate-limiting policies applied via <c>[EnableRateLimiting]</c>.</summary>
public static class RateLimitingPolicies
{
    /// <summary>Token-bucket limiter protecting the create endpoint, keyed by client IP.</summary>
    public const string Create = "create-policy";
}
