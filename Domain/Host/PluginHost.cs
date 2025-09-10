using CustomGenerics;
using Domain.Plugin;
using System.Linq.Expressions;

namespace Domain.Host;

// this is a code smell. The current desgin will require seperated unit test for both containers and plugin loader for one purpose like unload plugin - the reason is it cannot be sure which one should be take that responsible. But, this design is the only way allow intercepting, which allow writing unit test. For this reason, container when insert will take responsilbe to start plugin and manage DI services provide by plugin.
public interface IPluginContainer :
    IDisposable, IAsyncDisposable,
    IRepository<IPlugin>, IRepositoryAsync<IPlugin>
{

}

// this is marker interface to provide data for loading plugin
public interface IPluginSource { }

public interface IPluginLoader
{
    IPlugin LoadPlugin(IPluginSource pluginSource);
    void UnloadPlugin(IPlugin plugin);
    Task<IPlugin> LoadPluginAsync(IPluginSource pluginSource);
    Task UnloadPluginAsync(IPlugin plugin);
}