using Microsoft.Extensions.DependencyInjection;
using PosNfce.SharedKernel;

namespace PosNfce.Desktop.Shared;

/// <summary>
/// In-process event dispatch: every <see cref="IEventHandler{TEvent}"/> registered in DI runs, one after the other,
/// in its own scope (handlers belong to other modules and use their own DbContext).
/// </summary>
public sealed class ServiceProviderEventPublisher(IServiceScopeFactory scopes) : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        await using var scope = scopes.CreateAsyncScope();
        foreach (var handler in scope.ServiceProvider.GetServices<IEventHandler<TEvent>>())
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
    }
}
