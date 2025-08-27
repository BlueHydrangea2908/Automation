using System.Reflection.Metadata;

namespace Domain.Contracts.DI;

public interface IServiceResolver
{
    TService? Resolve<TService() where T : class;
    object? Resolve(Type serviceType);
}

public interface IServiceScope : IDisposable
{
    IServiceResolver Services { get; }
}

public interface IServiceScopeFactory
{
    IServiceScope CreateScope();
}

public enum ServiceLifetime
{
    Singleton,
    Scoped,
    Transient
}

// TService must be interface to ensure safely unload which c# generic cannot do. For now, all implements of IServiceRegistrar must throw exception if TService isn't interface. A roslyn sdk for project template will be provided soon to maintain this logic in compile time.
public interface IServiceRegistrar
{
    void RegisterFactory<TService>(Func<IServiceResolver, TService> factory, ServiceLifetime lifetime);
}


public interface IServiceCollection : IServiceRegistrar, IServiceResolver { }