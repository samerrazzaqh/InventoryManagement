using FluentValidation;
using InventoryManagement.Application.Caching;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Services;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        TypeAdapterConfig.GlobalSettings.Scan(assembly);
        services.AddMapster();
        services.AddValidatorsFromAssembly(assembly);

        // Domain-event handlers: IDomainEventHandler<T> implementations are discovered by convention.
        var handlerInterface = typeof(IDomainEventHandler<>);
        foreach (var type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
            foreach (var i in type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface))
                services.AddScoped(i, type);

        // Services + caching decorators (explicit composition: no Scrutor dependency, trivially debuggable).
        services.AddScoped<CategoryService>();
        services.AddScoped<ICategoryService>(sp =>
            new CachedCategoryService(sp.GetRequiredService<CategoryService>(), sp.GetRequiredService<ICacheService>()));

        services.AddScoped<ProductService>();
        services.AddScoped<IProductService>(sp =>
            new CachedProductService(sp.GetRequiredService<ProductService>(), sp.GetRequiredService<ICacheService>()));

        services.AddScoped<IStockService, StockService>();

        return services;
    }
}
