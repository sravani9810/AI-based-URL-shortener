namespace UrlShortener.Domain.Interfaces;

/// <summary>
/// Reversible mapping between a numeric id and its short code.
/// Implementation is Base62 over the row's primary key (ADR-0007).
/// </summary>
public interface ICodeGenerator
{
    /// <summary>Encodes a non-negative id into a compact short code.</summary>
    string Encode(long id);

    /// <summary>Decodes a short code back to its id. Inverse of <see cref="Encode"/>.</summary>
    long Decode(string code);
}
