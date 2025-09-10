using Domain.Host;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Tests.Host;

public abstract partial class PluginLoaderTestBase
{
}

partial class PluginLoaderTestBase
{
    public void LoadPlugin_MustProvideExportedServiceContracts(IPluginLoader pluginLoader, IPluginSource pluginSource)
    {
        var plugin = pluginLoader.LoadPlugin(pluginSource);

        // Assert
        Assert.NotNull(plugin.ExportedServiceContracts);
        Assert.All(plugin.ExportedServiceContracts, contract =>
        {
            Assert.True(contract.IsInterface || contract.IsAbstract,
                $"Contract {contract.FullName} must be interface or abstract class.");
        });
    }
}