using Domain.Entities.Host;

namespace Domain.Test.Entities.Host;

public abstract partial class PluginHostTestBase
{
    protected abstract IPluginHost GetHost();
}

// define unit test for cleaning up everything
partial class PluginHostTestBase
{

}