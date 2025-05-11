using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Core;

public interface ICommandAsync<TIn, TOut>
{
    public Task<TOut> ExecuteAsync(TIn input, CancellationToken token);
}
