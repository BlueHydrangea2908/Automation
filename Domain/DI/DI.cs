namespace Domain.DI;

/// <summary>
/// Factory for creating new service scopes.
/// A scope controls the lifetime of scoped services.
/// </summary>
public interface IServiceScopeFactory
{
    IServiceScope CreateScope();
}

/// <summary>
/// Service lifetime definitions, consistent with Microsoft.Extensions.DependencyInjection.
/// </summary>
public enum ServiceLifetime
{
    Singleton,
    Scoped,
    Transient
}

/// <summary>
/// Represents a service scope, which provides a resolver for scoped services
/// and disposes them when the scope ends.
/// </summary>
public interface IServiceScope : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// The resolver that can be used to resolve services within this scope.
    /// </summary>
    IServiceResolver ServiceResolver { get; }
}

/// <summary>
/// Registers factories for services. 
/// TService must be an interface to ensure safe unloading of plugins.
/// Implementations must throw if TService is not an interface.
/// </summary>
public interface IServiceRegistrar
{
    void RegisterFactory<TService>(
        Func<IServiceResolver, TService> factory,
        ServiceLifetime lifetime);

    /// <summary>
    /// Non-generic factory registration.
    /// This overload is intended for testing purposes only
    /// and does not enforce the interface restriction.
    /// </summary>
    void RegisterFactory(
        Type serviceType,
        Func<IServiceResolver, object> factory,
        ServiceLifetime lifetime);
}

/// <summary>
/// Resolves services by type.
/// </summary>
public interface IServiceResolver
{
    TService? Resolve<TService>() where TService : class;
    object? Resolve(Type serviceType);
}

/// <summary>
/// The root service container, which acts as a registrar, resolver,
/// and (typically) the entry point for creating scopes.
/// </summary>
public interface IServiceContainer :
    IServiceRegistrar,
    IServiceResolver,
    IServiceScopeFactory
{ }
