using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoList.Infra.Data.Services;

namespace TodoList.Infra.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlazorWasmDatabaseContextFactory<TContext>( 
        this IServiceCollection serviceCollection, 
        Action<DbContextOptionsBuilder>? optionsAction = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton) where TContext : DbContext
        => AddBlazorWasmDatabaseContextFactory<TContext>(
            serviceCollection,
            optionsAction == null ? null : (_, oa) => optionsAction(oa),
            lifetime);

    public static IServiceCollection AddBlazorWasmDatabaseContextFactory<TContext>(
        this IServiceCollection serviceCollection,
        Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TContext : DbContext
    {
        serviceCollection.TryAdd(
            new ServiceDescriptor(
                typeof(IDatabaseStorageService),
                typeof(BrowserCacheDatabaseStorageService),
                ServiceLifetime.Singleton));

        serviceCollection.TryAdd(
            new ServiceDescriptor(
                typeof(IDatabaseSwapService),
                typeof(DatabaseSwapService),
                ServiceLifetime.Singleton));

        serviceCollection.TryAdd(
            new ServiceDescriptor(
                typeof(IBlazorWasmDbContextFactory<TContext>),
                typeof(BlazorWasmDbContextFactory<TContext>),
                ServiceLifetime.Singleton));

        serviceCollection.AddDbContextFactory<TContext>(
            optionsAction ?? ((_, _) => { }), lifetime);

        return serviceCollection;
    }
}