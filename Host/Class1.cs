using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Linq;
using System.Reflection;

[AttributeUsage(AttributeTargets.Assembly)]
public class PluginIdAttribute : Attribute
{
    public Guid Id { get; }
    public PluginIdAttribute(string guid) => Id = Guid.Parse(guid);
}

public class LifetimeTrackingProxy<T> : DispatchProxy where T : class
{
    private T? _target;
    private Guid _ownerId;
    private Func<bool>? _isAlive;

    public static T Create(T target, Guid ownerId, Func<bool> isAlive)
    {
        var proxy = Create<T, LifetimeTrackingProxy<T>>();
        var self = (LifetimeTrackingProxy<T>)(object)proxy;
        self.Init(target, ownerId, isAlive);
        return proxy;
    }

    private void Init(T target, Guid ownerId, Func<bool> isAlive)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _ownerId = ownerId;
        _isAlive = isAlive ?? throw new ArgumentNullException(nameof(isAlive));
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null)
            throw new ArgumentNullException(nameof(targetMethod));

        if (_isAlive == null || !_isAlive())
            throw new ObjectDisposedException($"Plugin {_ownerId} has been unloaded.");

        try
        {
            return targetMethod.Invoke(_target, args);
        }
        catch (TargetInvocationException tie)
        {
            Console.Error.WriteLine($"[Plugin {_ownerId}] Exception: {tie.InnerException}");
            throw tie.InnerException ?? tie;
        }
    }
}

public class ProxyExportDescriptorProvider : ExportDescriptorProvider
{
    private readonly ExportDescriptorProvider _inner;
    private readonly ConcurrentDictionary<Guid, List<WeakReference>> _pluginRefs = new();
    private readonly ConcurrentDictionary<Guid, bool> _pluginAlive = new();

    public ProxyExportDescriptorProvider(ExportDescriptorProvider inner)
    {
        _inner = inner;
    }

    public void RegisterPlugin(Guid pluginId)
    {
        _pluginAlive[pluginId] = true;
        _pluginRefs[pluginId] = new List<WeakReference>();
    }

    public void UnregisterPlugin(Guid pluginId)
    {
        _pluginAlive[pluginId] = false;
        _pluginRefs[pluginId].Clear();
    }

    public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(
      CompositionContract contract,
      DependencyAccessor descriptorAccessor)
    {
        foreach (var innerPromise in _inner.GetExportDescriptors(contract, descriptorAccessor))
        {
            yield return new ExportDescriptorPromise(
                contract,
                "Proxy for " + contract.ContractType?.Name,
                isShared: innerPromise.IsShared,
                dependencies: () => innerPromise.Dependencies,
                getDescriptor: deps =>
                {
                    var innerDescriptor = innerPromise.GetDescriptor();
                    return ExportDescriptor.Create((context, operation) =>
                    {
                        var realInstance = innerDescriptor.Activator(context, operation);
                        var ownerId = GetOwnerPluginId(realInstance);

                        var proxy = typeof(LifetimeTrackingProxy<>) 
                            .MakeGenericType(contract.ContractType ?? realInstance.GetType())
                            .GetMethod(nameof(LifetimeTrackingProxy<object>.Create))!
                            .Invoke(null, new object[] {
                                realInstance,
                                ownerId,
                                new Func<bool>(() => _pluginAlive.GetValueOrDefault(ownerId))
                            });

                        _pluginRefs[ownerId].Add(new WeakReference(proxy));
                        return proxy!;
                    },
                    innerDescriptor.Metadata);
                });
        }
    }

    private Guid GetOwnerPluginId(object instance)
    {
        var attr = instance.GetType().Assembly
            .GetCustomAttributes(typeof(PluginIdAttribute), false)
            .FirstOrDefault() as PluginIdAttribute;
        return attr?.Id ?? Guid.Empty;
    }
}
