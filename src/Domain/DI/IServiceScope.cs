namespace Domain.DI;

public interface IServiceScope : IDisposable
{
    IServiceResolver Services { get; }
}
