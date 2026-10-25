using Microsoft.Extensions.DependencyInjection;

namespace PosNfce.Desktop.Shared;

/// <summary>
/// Runs a use case in its own DI scope. Screens live for a long time, but each operation needs a fresh DbContext:
/// a long-lived one would keep stale data and grow with every change it tracks.
/// </summary>
public sealed class UseCaseRunner(IServiceScopeFactory scopes)
{
    public async Task<TResult> RunAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> action)
        where TUseCase : notnull
    {
        await using var scope = scopes.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<TUseCase>());
    }

    public async Task RunAsync<TUseCase>(Func<TUseCase, Task> action)
        where TUseCase : notnull
    {
        await using var scope = scopes.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<TUseCase>());
    }
}
