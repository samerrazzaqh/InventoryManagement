using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Infrastructure.Persistence;

/// <summary>In-process dispatcher. For cross-service guarantees, replace with a transactional outbox.</summary>
internal sealed class DomainEventDispatcher(IServiceProvider services, ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var domainEvent in events)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

            foreach (var handler in services.GetServices(handlerType))
            {
                try
                {
                    await (Task)method.Invoke(handler, [domainEvent, ct])!;
                }
                catch (Exception ex)
                {
                    // The data is already committed: a failing side-effect must not turn a successful request into an error.
                    logger.LogError(ex, "Domain event handler {Handler} failed for {Event}.", handler!.GetType().Name, domainEvent.GetType().Name);
                }
            }
        }
    }
}
