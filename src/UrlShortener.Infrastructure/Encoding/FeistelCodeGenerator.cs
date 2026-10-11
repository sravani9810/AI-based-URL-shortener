using System.Security.Cryptography;
using UrlShortener.Domain.Interfaces;

namespace UrlShortener.Infrastructure.Encoding;

/// <summary>
/// Makes short codes non-enumerable (ADR-0007 mitigation) by passing the sequential row id through a
/// keyed Feistel permutation before Base62 encoding. The permutation is a bijection over a fixed
/// 48-bit domain, so codes stay globally unique and collision-free while appearing random; it is
/// reversible, so <see cref="Decode"/> still works. Delegates the actual Base62 to an inner generator.
/// </summary>
public sealed class FeistelCodeGenerator : ICodeGenerator
{
    private const int HalfBits = 24;
    private const uint HalfMask = (1u << HalfBits) - 1;        // 24-bit half
    private const long DomainMask = (1L << (HalfBits * 2)) - 1; // 48-bit domain
    private const int Rounds = 4;

    private readonly ICodeGenerator _inner; // Base62
    private readonly uint[] _roundKeys;

    public FeistelCodeGenerator(ICodeGenerator inner, string secret)
    {
        _inner = inner;
        _roundKeys = DeriveRoundKeys(secret);
    }

    public string Encode(long id)
    {
        if (id < 0 || id > DomainMask)
            throw new ArgumentOutOfRangeException(nameof(id), $"Id must be in [0, {DomainMask}].");
        return _inner.Encode(Permute(id));
    }

    public long Decode(string code) => Unpermute(_inner.Decode(code));

    // Balanced Feistel: L_i = R_{i-1}, R_i = L_{i-1} XOR F(R_{i-1}, k_i).
    private long Permute(long value)
    {
        var l = (uint)((value >> HalfBits) & HalfMask);
        var r = (uint)(value & HalfMask);
        for (var i = 0; i < Rounds; i++)
        {
            var next = (l ^ F(r, _roundKeys[i])) & HalfMask;
            l = r;
            r = next;
        }
        return ((long)l << HalfBits) | r;
    }

    // Inverse of Permute: run the rounds in reverse.
    private long Unpermute(long value)
    {
        var l = (uint)((value >> HalfBits) & HalfMask);
        var r = (uint)(value & HalfMask);
        for (var i = Rounds - 1; i >= 0; i--)
        {
            var prev = (r ^ F(l, _roundKeys[i])) & HalfMask;
            r = l;
            l = prev;
        }
        return ((long)l << HalfBits) | r;
    }

    private static uint F(uint half, uint key)
    {
        // Deterministic mixing function; any function keeps Feistel invertible.
        ulong x = ((ulong)(half ^ key) + 0x9E3779B1UL) * 2654435761UL;
        x ^= x >> 15;
        return (uint)(x & HalfMask);
    }

    private static uint[] DeriveRoundKeys(string secret)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret));
        var keys = new uint[Rounds];
        for (var i = 0; i < Rounds; i++)
            keys[i] = BitConverter.ToUInt32(hash, i * 4) & HalfMask;
        return keys;
    }
}
