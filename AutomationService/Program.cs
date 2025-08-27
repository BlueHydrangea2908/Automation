using Automation.Core;
using AutomationService;
using System.Security.Cryptography.X509Certificates;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

public interface IAutomationPipe : IEdge<IDictionary<string, object>>
{ }

public abstract class  AbstractAutomationPipe : IAutomationPipe
{
    public AbstractAutomationPipe()
    {
        // Initialization logic if needed
    }

    abstract public Task<IEdge<IDictionary<string, object>>> PumpAsync(IDictionary<string, object> fluid, CancellationToken token);
}

public class Macro : IEdge<> { }
public class Function : IEdge<> { } 
public interface IRepository<T>
{
    public Task<IEnumerable<T>> SelectAsync(Func<T, bool> predicate, Func<T, T> func, CancellationToken token);
    public Task<IEnumerable<T>> WhereAsync(Func<T, bool> predicate, CancellationToken token);/
    public Task InsertAsync(Func<T, T> func, CancellationToken token);
    public Task UpdateAsync(Func<T, bool> predicate, Func<T, T> func, CancellationToken token);
    public Task DeleteAsync(Func<T, bool> predicate, CancellationToken token);
}
public class Manager : IRepository<IEdge<>>
{
    public Task<IEnumerable<IEdge<object>>> SelectAsync(Func<IEdge<object>, bool> predicate, Func<IEdge<object>, object> func, CancellationToken token)
    {
        throw new NotImplementedException();
    }
    public Task<IEnumerable<IEdge<object>>> WhereAsync(Func<IEdge<object>, bool> predicate, CancellationToken token)
    {
        throw new NotImplementedException();
    }
    public Task InsertAsync(Func<IEdge<object>, object> func, CancellationToken token)
    {
        throw new NotImplementedException();
    }
    public Task UpdateAsync(Func<IEdge<object>, bool> predicate, Func<IEdge<object>, object> func, CancellationToken token)
    {
        throw new NotImplementedException();
    }
    public Task DeleteAsync(Func<IEdge<object>, bool> predicate, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}