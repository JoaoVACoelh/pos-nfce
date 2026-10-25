namespace PosNfce.SharedKernel;

/// <summary>Publishes an event to every handler registered for it, in-process. The implementation lives in the composition root.</summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}

/// <summary>Reacts to an event published by another part of the system (e.g. Fiscal handling <c>SaleCompleted</c>).</summary>
public interface IEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
