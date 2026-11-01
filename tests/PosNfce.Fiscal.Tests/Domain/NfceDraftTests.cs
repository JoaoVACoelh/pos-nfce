using PosNfce.Fiscal.Domain.Documents;
using PosNfce.Fiscal.Domain.Issuers;
using PosNfce.Fiscal.Domain.Taxes;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests.Domain;

public class NfceDraftTests
{
    private static readonly DateTimeOffset IssuedAt = new(2026, 10, 25, 10, 0, 0, TimeSpan.FromHours(-3));

    private static FiscalDocument NumberedDocument(params FiscalPayment[] payments)
    {
        var document = FiscalDocument.FromSale(
            Guid.NewGuid(), IssuedAt,
            [FiscalDocumentTests.Item(1, 10m) with { Description = "Refrigerante" }, FiscalDocumentTests.Item(2, 20m) with { Description = "Pão" }],
            new Money(3m), new Money(27m), new Money(3m),
            payments.Length > 0 ? payments : [new FiscalPayment("Cash", new Money(30m), false)]);
        document.AssignNumber(1, 15);
        return document;
    }

    private static ItemTaxes Taxes(decimal federal, decimal state) => new(
        new SimplesIcms(SimplesIcmsGroup.Sn102, "102", 0),
        new PisCofins(PisCofinsGroup.Other, "49", Money.Zero, 0m, Money.Zero),
        new PisCofins(PisCofinsGroup.Other, "49", Money.Zero, 0m, Money.Zero),
        new ApproximateTaxes(new Money(federal), new Money(state), Money.Zero),
        null);

    private static NfceDraft Build(FiscalDocument document, bool homologation = true) =>
        NfceDraft.Build(document, IssuerTests.Profile(), [Taxes(1.21m, 1.62m), Taxes(2m, 3m)], IssuedAt, homologation, "12345678", "IBPT 26.2.A");

    [Fact]
    public void In_homologation_the_first_item_gets_the_fixed_description()
    {
        var draft = Build(NumberedDocument());

        Assert.Equal([NfceDraft.HomologationDescription, "Pão"], draft.Items.Select(i => i.Description));
    }

    [Fact]
    public void In_production_descriptions_are_kept()
    {
        Assert.Equal("Refrigerante", Build(NumberedDocument(), homologation: false).Items[0].Description);
    }

    [Fact]
    public void Totals_add_items_discount_and_approximate_taxes()
    {
        var totals = Build(NumberedDocument()).Totals;

        Assert.Equal((30m, 3m, 27m), (totals.Products.Amount, totals.Discount.Amount, totals.Total.Amount));
        Assert.Equal((3.21m, 4.62m, 7.83m), (totals.Approximate.Federal.Amount, totals.Approximate.State.Amount, totals.Approximate.Total.Amount));
        Assert.False(totals.HasIbsCbs);
    }

    [Fact]
    public void Approximate_taxes_text_follows_lei_12741()
    {
        Assert.Equal("Trib aprox R$ 3,21 Federal, R$ 4,62 Estadual e R$ 0,00 Municipal. Fonte: IBPT 26.2.A", Build(NumberedDocument()).AdditionalInfo);
    }

    [Fact]
    public void Payments_get_the_nfe_codes_and_cards_get_the_card_group()
    {
        var draft = Build(NumberedDocument(
            new FiscalPayment("DebitCard", new Money(10m), false),
            new FiscalPayment("Pix", new Money(7m), false),
            new FiscalPayment("Cash", new Money(13m), false)));

        Assert.Equal([NfcePaymentKind.DebitCard, NfcePaymentKind.Pix, NfcePaymentKind.Cash], draft.Payments.Select(p => p.Kind));
        Assert.Equal([true, true, false], draft.Payments.Select(p => p.HasCardGroup));
        Assert.Equal([4, 17, 1], draft.Payments.Select(p => (int)p.Kind));
    }

    [Fact]
    public void Document_without_number_can_not_be_built()
    {
        var document = FiscalDocument.FromSale(
            Guid.NewGuid(), IssuedAt, [FiscalDocumentTests.Item(1, 10m)], Money.Zero, new Money(10m), Money.Zero,
            [new FiscalPayment("Cash", new Money(10m), false)]);

        Assert.Throws<DomainException>(() => NfceDraft.Build(document, IssuerTests.Profile(), [Taxes(0m, 0m)], IssuedAt, true, "12345678", null));
    }
}
