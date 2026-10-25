using PosNfce.Catalog.Domain.Products;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Tests.Domain;

public class NcmAndCestTests
{
    [Theory]
    [InlineData("22021000", "22021000")]
    [InlineData("2202.10.00", "22021000")]
    public void Ncm_accepts_eight_digits_with_or_without_dots(string input, string expected)
    {
        Assert.Equal(expected, Ncm.Parse(input).Value);
    }

    [Theory]
    [InlineData("2202100")]
    [InlineData("220210000")]
    [InlineData("2202A000")]
    [InlineData("")]
    public void Ncm_rejects_anything_else(string input)
    {
        Assert.Throws<DomainException>(() => Ncm.Parse(input));
    }

    [Fact]
    public void Cest_is_optional()
    {
        Assert.Null(Cest.ParseOptional(""));
        Assert.Null(Cest.ParseOptional(null));
    }

    [Fact]
    public void Cest_accepts_seven_digits_with_dots()
    {
        Assert.Equal("0300700", Cest.ParseOptional("03.007.00")!.Value);
    }

    [Theory]
    [InlineData("030070")]
    [InlineData("03007000")]
    public void Cest_rejects_wrong_length(string input)
    {
        Assert.Throws<DomainException>(() => Cest.ParseOptional(input));
    }
}
