namespace Domain.DI;

public interface IServiceResolver
{
    TService? Resolve<TService() where T : class;
    object? Resolve(Type serviceType);
}
