namespace UrlShortener.Application.Exceptions;

/// <summary>Input failed validation. Maps to HTTP 400.</summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
