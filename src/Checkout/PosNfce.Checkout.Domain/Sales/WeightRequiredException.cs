using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Domain.Sales;

/// <summary>A product sold by the kilogram was typed without its weight. The checkout asks for it and keeps the code.</summary>
public sealed class WeightRequiredException(string description)
    : DomainException($"\"{description}\" é vendido por KG: digite o peso (ex.: 0,350) e tecle Enter. Esc desiste.");
