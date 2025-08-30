using CustomGenerics;
using Domain.Plugin;
using System.Linq.Expressions;

namespace Domain.Host;

public interface IPluginContainer :
    IDisposable, IAsyncDisposable,
    IRepository<IPlugin>, IRepositoryAsync<IPlugin>
{

}

public interface IPluginLoader<TPluginSource>
{
    IPlugin LoadPlugin(TPluginSource pluginSource);
    void UnloadPlugin(IPlugin plugin);
}

public interface IPluginLoaderAsync<TPluginSource>
{
    Task<IPlugin> LoadPluginAsync(TPluginSource pluginSource);
    Task UnloadPluginAsync(IPlugin plugin);
}