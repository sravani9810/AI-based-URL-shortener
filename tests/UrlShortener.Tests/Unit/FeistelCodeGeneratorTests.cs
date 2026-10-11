using UrlShortener.Infrastructure.Encoding;
using Xunit;

namespace UrlShortener.Tests.Unit;

public class FeistelCodeGeneratorTests
{
    private readonly FeistelCodeGenerator _sut = new(new Base62CodeGenerator(), "test-secret");

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(1000)]
    [InlineData(123456789)]
    [InlineData((1L << 48) - 1)] // top of the domain
    public void Encode_then_Decode_round_trips(long id) =>
        Assert.Equal(id, _sut.Decode(_sut.Encode(id)));

    [Fact]
    public void Codes_are_unique_across_a_range()
    {
        var codes = Enumerable.Range(1, 5000).Select(i => _sut.Encode(i)).ToList();
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void Codes_are_not_sequential()
    {
        // Plain Base62 would yield "1","2","3"; the permutation must not.
        Assert.NotEqual("1", _sut.Encode(1));
        Assert.NotEqual("2", _sut.Encode(2));
        // Consecutive ids must not produce adjacent-looking codes.
        Assert.NotEqual(_sut.Encode(1), _sut.Encode(2));
    }

    [Fact]
    public void Out_of_domain_id_throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Encode(1L << 48));

    [Fact]
    public void Different_secrets_produce_different_codes()
    {
        var other = new FeistelCodeGenerator(new Base62CodeGenerator(), "another-secret");
        Assert.NotEqual(_sut.Encode(42), other.Encode(42));
    }
}
