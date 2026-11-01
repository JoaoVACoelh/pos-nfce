using PosNfce.Checkout.Domain.Sales;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Tests.Domain;

public class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    public void Valid_cpf_with_or_without_mask(string input)
    {
        var cpf = Cpf.Parse(input);

        Assert.Equal(("52998224725", "529.982.247-25"), (cpf.Value, cpf.Formatted));
    }

    [Theory]
    [InlineData("529.982.247-26")] // wrong check digit
    [InlineData("5299822472")]     // 10 digits
    [InlineData("111.111.111-11")] // repeated digits pass the math but aren't valid
    public void Invalid_cpf_is_refused(string input)
    {
        Assert.Throws<DomainException>(() => Cpf.Parse(input));
    }

    [Theory]
    [InlineData("111.111.111-11", "confira os números")]
    [InlineData("529.982.247-26", "confira os números")]
    [InlineData("5299822472", "11 dígitos")]
    public void Refusal_says_what_is_wrong(string input, string expected)
    {
        Assert.Contains(expected, Assert.Throws<DomainException>(() => Cpf.Parse(input)).Message);
    }
}
