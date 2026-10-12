namespace UrlShortener.Application.Exceptions;

/// <summary>A uniqueness conflict (e.g. a custom alias already in use). Maps to HTTP 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
