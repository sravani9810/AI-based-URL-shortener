using UrlShortener.Domain.Interfaces;

namespace UrlShortener.Infrastructure.Encoding;

/// <summary>
/// Base62 ([0-9A-Za-z]) encoder/decoder used to turn a row's primary key into a short code
/// and back (ADR-0007). Pure and deterministic; unit-tested for round-trip correctness.
/// </summary>
public sealed class Base62CodeGenerator : ICodeGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int Base = 62;

    public string Encode(long id)
    {
        if (id < 0)
            throw new ArgumentOutOfRangeException(nameof(id), "Id must be non-negative.");
        if (id == 0)
            return Alphabet[0].ToString();

        // Build the code from least- to most-significant digit, then reverse.
        Span<char> buffer = stackalloc char[11]; // enough for long.MaxValue in base 62
        var pos = buffer.Length;
        var value = id;
        while (value > 0)
        {
            buffer[--pos] = Alphabet[(int)(value % Base)];
            value /= Base;
        }
        return new string(buffer[pos..]);
    }

    public long Decode(string code)
    {
        if (string.IsNullOrEmpty(code))
            throw new ArgumentException("Code must not be empty.", nameof(code));

        long value = 0;
        foreach (var c in code)
        {
            var digit = Alphabet.IndexOf(c);
            if (digit < 0)
                throw new FormatException($"Invalid Base62 character '{c}'.");
            value = checked(value * Base + digit);
        }
        return value;
    }
}
