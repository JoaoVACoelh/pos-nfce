namespace PosNfce.Fiscal.Domain.Issuers;

/// <summary>Tax regime code (<c>CRT</c>). Only Simples Nacional is supported for now; other regimes come with their tax strategy.</summary>
public enum TaxRegime
{
    SimplesNacional = 1,
}
