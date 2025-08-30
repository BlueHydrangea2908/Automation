using System.Reflection.Metadata;

namespace Domain.DI;

public interface IServiceContainer : IServiceRegistrar, IServiceResolver { }