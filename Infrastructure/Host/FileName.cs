using CustomGenerics;
using Domain.Contracts.DI;
using Domain.Entities.Host;
using Domain.Entities.Plugin;
using Infrastructure.DI;

namespace Infrastructure.Host;

// this is marker interface to provide data for loading plugin
public interface IPluginSource { }
public abstract class PluginHostBase : IPluginHost
{

    public PluginHostBase(IServiceCollection serviceCollection, IRepositoryAsync<IPlugin> pluginRepos)
    {
        ServiceCollection = serviceCollection;
        PluginRepos = pluginRepos;
    }


    private bool disposedValue;

    protected abstract IAsyncFactory<IPlugin, IPluginSource> PluginFactory { get; set; }

    protected IRepositoryAsync<IPlugin> PluginRepos { get; private set; }

    public async Task LoadPluginAsync(IPluginSource pluginSource, CancellationToken token)
    {
        var plugin = await PluginFactory.GetAsync(pluginSource, token);
        plugin.ConfigureServices(ServiceCollection);
        await plugin.StartAsync(ServiceCollection, token);
        await PluginRepos.InsertAsync(new List<IPlugin>() { plugin }, token);
    }

    public EnsureTServiceIsInterfaceServiceCollectionBase ServiceCollection { get; private set; }

    public abstract Task UnloadPluginAsync(IPluginIdentity id, CancellationToken token);

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~PluginHostBase()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}