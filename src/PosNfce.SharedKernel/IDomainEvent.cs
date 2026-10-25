namespace PosNfce.SharedKernel;

/// <summary>Something that happened in a module's domain and that other parts of the system may react to.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
