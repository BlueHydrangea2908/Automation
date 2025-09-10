using Domain.DI;

namespace Domain.Plugin;
public interface IPluginDescription
{
    public string Name { get; }
    public string Version { get; }
    public string Author { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
}

public interface IExporterServiceContractsAccessable
{
    IEnumerable<Type> ExportedServiceContracts { get; }
}

public interface IPluginIdentity : IEquatable<IPluginIdentity>
{
    //public Guid Guid { get; }
}

public interface IPlugin : IPluginIdentity, IPluginDescription, IDisposable, IAsyncDisposable, IExporterServiceContractsAccessable
{
    public void ConfigureServices(IServiceRegistrar registrar);
    public Task StartAsync(IServiceResolver resolver, CancellationToken token);
}
