using UrlShortener.Api.Services;

namespace UrlShortener.Tests.UnitTests;

public class ShortCodeGeneratorTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(62)]
    [InlineData(999999)]
    public void FromId_ValidId_ReturnsSixCharCode(long id)
        => Assert.Equal(6, ShortCodeGenerator.FromId(id).Length);

    [Fact]
    public void FromId_GeneratesUniqueCodesForSequentialIds()
    {
        var codes = Enumerable.Range(1, 500)
            .Select(i => ShortCodeGenerator.FromId(i))
            .ToList();
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Theory]
    [InlineData(0), InlineData(-1)]
    public void FromId_InvalidId_ThrowsArgumentOutOfRange(long id)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => ShortCodeGenerator.FromId(id));

    [Fact]
    public void FromId_ReturnsOnlyUrlSafeCharacters()
        => Assert.Matches(@"^[a-zA-Z0-9]+$",
            ShortCodeGenerator.FromId(12345));
}