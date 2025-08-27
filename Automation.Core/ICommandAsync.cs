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

public abstract class Expression<TScope, TValue> : ICommandAsync<TScope, TValue>
{
    protected abstract Task<TValue> InterpretAsync(TScope scope, CancellationToken token);
    public Task<TValue> ExecuteAsync(TScope input, CancellationToken token) => InterpretAsync(input, token);
}

public interface IEdge<TFluid>
{
    // pump return another pump instance to preapre for desinging fallback, repeat if fail and quit when error
    public Task<IEdge<TFluid>> PumpAsync(TFluid fluid, CancellationToken token);
}