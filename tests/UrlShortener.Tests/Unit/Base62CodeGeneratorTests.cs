using UrlShortener.Infrastructure.Encoding;
using Xunit;

namespace UrlShortener.Tests.Unit;

public class Base62CodeGeneratorTests
{
    private readonly Base62CodeGenerator _sut = new();

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(61, "z")]
    [InlineData(62, "10")]
    [InlineData(3843, "zz")]   // 62*62 - 1
    public void Encode_produces_expected_code(long id, string expected) =>
        Assert.Equal(expected, _sut.Encode(id));

    [Theory]
    [InlineData(1)]
    [InlineData(62)]
    [InlineData(12345)]
    [InlineData(9_999_999_999)]
    [InlineData(long.MaxValue)]
    public void Encode_then_Decode_round_trips(long id) =>
        Assert.Equal(id, _sut.Decode(_sut.Encode(id)));

    [Fact]
    public void Encode_negative_id_throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Encode(-1));

    [Fact]
    public void Decode_invalid_character_throws() =>
        Assert.Throws<FormatException>(() => _sut.Decode("abc!"));
}
