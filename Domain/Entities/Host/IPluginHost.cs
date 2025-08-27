using CustomGenerics;
using Domain.Entities.Plugin;

namespace Domain.Entities.Host;

public interface IPluginHost : IDisposable, IAsyncDisposable, IRepository<IPlugin>, IRepositoryAsync<IPlugin>
{

}