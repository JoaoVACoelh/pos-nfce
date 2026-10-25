using PosNfce.Catalog.Domain.Products;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Tests.Domain;

public class GtinTests
{
    [Theory]
    [InlineData("12345670")]       // GTIN-8
    [InlineData("036000291452")]   // GTIN-12 (UPC-A)
    [InlineData("7891234567895")]  // GTIN-13 (EAN-13)
    [InlineData("17891234567892")] // GTIN-14
    public void Accepts_valid_barcodes(string value)
    {
        var gtin = Gtin.Parse(value);

        Assert.True(gtin.HasBarcode);
        Assert.Equal(value, gtin.Value);
    }

    [Theory]
    [InlineData("7891234567894")] // wrong check digit
    [InlineData("789123456789")]  // 12 digits, wrong check digit for GTIN-12
    [InlineData("1234567")]       // invalid length
    [InlineData("78912345678A5")] // letter
    public void Rejects_invalid_barcodes(string value)
    {
        Assert.Throws<DomainException>(() => Gtin.Parse(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("sem gtin")]
    public void Empty_or_sem_gtin_means_no_barcode(string? value)
    {
        var gtin = Gtin.Parse(value);

        Assert.False(gtin.HasBarcode);
        Assert.Equal("SEM GTIN", gtin.Value);
    }
}
