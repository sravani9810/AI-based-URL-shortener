namespace UrlShortener.Domain.Exceptions;

/// <summary>
/// Raised when a code/alias insert violates the unique index — i.e. a concurrent request already
/// took that code between our existence check and the write (a check-then-act race). Maps to HTTP 409.
/// </summary>
public class DuplicateCodeException : Exception
{
    public DuplicateCodeException(string message, Exception? inner = null) : base(message, inner) { }
}
