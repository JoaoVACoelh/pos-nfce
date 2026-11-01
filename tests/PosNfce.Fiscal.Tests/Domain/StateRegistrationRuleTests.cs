using PosNfce.Fiscal.Domain.Documents;
using PosNfce.Fiscal.Domain.Issuers;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests.Domain;

public sealed class StateRegistrationRuleTests
{
    private static readonly BrazilianState Mg = BrazilianState.Parse("MG");

    [Theory]
    [InlineData("062.307.904/0081")] // example published with the MG layout
    [InlineData("0623079040081")]
    [InlineData("0000000000000")]
    public void Valid_mg_registration_is_accepted_as_digits(string typed)
    {
        Assert.Matches("^[0-9]{13}$", StateRegistrationRule.Validate(typed, Mg));
    }

    [Theory]
    [InlineData("0623079040082")] // second check digit wrong
    [InlineData("0623079040071")] // first check digit wrong
    [InlineData("062307904008")]  // 12 digits
    [InlineData("")]
    public void Invalid_mg_registration_is_refused(string typed)
    {
        Assert.Throws<DomainException>(() => StateRegistrationRule.Validate(typed, Mg));
    }

    [Fact]
    public void Other_states_keep_the_length_check_only()
    {
        Assert.Equal("123456789", StateRegistrationRule.Validate("123456789", BrazilianState.Parse("SP")));
        Assert.Throws<DomainException>(() => StateRegistrationRule.Validate("1", BrazilianState.Parse("SP")));
    }

    [Fact]
    public void Mg_requires_the_consumer_from_ten_thousand_reais_inclusive()
    {
        Assert.Equal(new Money(10_000m), ConsumerIdentificationRule.MandatoryFrom(Mg));
        ConsumerIdentificationRule.Check(new Money(9_999.99m), null, Mg);
        ConsumerIdentificationRule.Check(new Money(10_000m), "52998224725", Mg);
        Assert.Throws<DomainException>(() => ConsumerIdentificationRule.Check(new Money(10_000m), null, Mg));
    }

    [Fact]
    public void States_not_verified_yet_have_no_limit()
    {
        Assert.Null(ConsumerIdentificationRule.MandatoryFrom(BrazilianState.Parse("SP")));
    }
}
