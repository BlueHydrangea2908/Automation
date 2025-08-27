using Domain.Contracts.DI;

namespace Domain.Entities.Plugin;
public interface IPluginDescription
{
    public string Name { get; }
    public string Version { get; }
    public string Author { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
}

public interface IPluginIdentity : IEquatable<IPluginIdentity>
{
    public Guid Guid { get; }
}


public interface IPluginMetadata : IPluginIdentity, IPluginDescription
{


}


public interface IPlugin : IPluginMetadata, IDisposable, IAsyncDisposable
{
    public void ConfigureServices(IServiceRegistrar registrar);
    public Task StartAsync(IServiceResolver resolver, CancellationToken token);

}
