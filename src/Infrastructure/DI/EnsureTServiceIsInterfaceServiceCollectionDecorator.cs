using Domain.DI;

namespace Infrastructure.DI;

public class EnsureTServiceIsInterfaceServiceCollectionDecorator : IServiceContainer
{
    private readonly IServiceContainer _inner;

    public EnsureTServiceIsInterfaceServiceCollectionDecorator(IServiceContainer inner)
    {
        _inner = inner;
    }

    public void RegisterFactory<TService>(Func<IServiceResolver, TService> factory, ServiceLifetime lifetime)
    {
        if (!typeof(TService).IsInterface)
            throw new InvalidOperationException("TService must be an interface to ensure unload safety");
        _inner.RegisterFactory<TService>(factory, lifetime);
    }

    public TService? Resolve<TService>()
    {
        if (!typeof(TService).IsInterface)
            throw new InvalidOperationException("TService must be an interface to ensure unload safety");
        return _inner.Resolve<TService>();
    }

    public object? Resolve(Type serviceType)
    {
        if (!serviceType.IsInterface)
            throw new InvalidOperationException("serviceType must be an interface to ensure unload safety");
        return _inner.Resolve(serviceType);
    }
}