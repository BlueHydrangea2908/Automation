using Castle.DynamicProxy;
using Domain.DI;
using Domain.Host;
using Domain.Plugin;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Domain.Tests.Host;

public abstract partial class PluginContainerTestBase
{
    protected Random Randomizer = new Random();
}

partial class PluginContainerTestBase
{
    public virtual void Insert_ShouldCallStartAsyncAndConfigureServiceWithAllInsertedPlugins_WhenPluginsDoNotRequireServicesFromOtherPlguin(IPluginContainer pluginContainer)
    {
        var numberOfPlugins = this.Randomizer.Next(10, 50);
        var mockers = new List<Mock<IPlugin>>();
        for (int i = 0; i < numberOfPlugins; i++)
        {
            var newPluginMock = new Mock<IPlugin>();

            newPluginMock
                .Setup(p => p.StartAsync(It.IsAny<IServiceResolver>(), It.IsAny<CancellationToken>()))
                .Verifiable();

            newPluginMock
                .Setup(p => p.ConfigureServices(It.IsAny<IServiceRegistrar>()))
                .Verifiable();

            mockers.Add(newPluginMock);
        }

        pluginContainer.Insert(mockers.Select(m => m.Object));

        foreach (var mocker in mockers)
        {
            mocker.Verify(
                p => p.ConfigureServices(It.IsAny<IServiceRegistrar>()),
                Times.Once
            );
            mocker.Verify(
                p => p.StartAsync(It.IsAny<IServiceResolver>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }
    }

    public virtual void Insert_MustSelfAdjustLoadOrder_WhenPluginsDependOnEachOther(IPluginContainer pluginContainer)
    {
        // ---------- Reflection.Emit module ----------
        var asmName = new AssemblyName("DynamicServices_" + Guid.NewGuid().ToString("N"));
        var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
        var modBuilder = asmBuilder.DefineDynamicModule("MainModule");

        Type CreateServiceInterface(string name)
        {
            var tb = modBuilder.DefineType(
                name,
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);

            // single method: string GetId()
            tb.DefineMethod(
                "GetId",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual,
                typeof(string),
                Type.EmptyTypes);

            return tb.CreateTypeInfo()!;
        }

        // ---------- Castle interceptor (returns fixed string id for any method returning string) ----------
        Type BuildStringReturnInterceptorType()
        {
            var tb = modBuilder.DefineType(
                "StringReturnInterceptor_" + Guid.NewGuid().ToString("N"),
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);

            tb.AddInterfaceImplementation(typeof(IInterceptor));

            var idField = tb.DefineField("_id", typeof(string), FieldAttributes.Private);

            // ctor(string id) { base(); _id = id; }
            var ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(string) });
            var ilc = ctor.GetILGenerator();
            ilc.Emit(OpCodes.Ldarg_0);
            ilc.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
            ilc.Emit(OpCodes.Ldarg_0);
            ilc.Emit(OpCodes.Ldarg_1);
            ilc.Emit(OpCodes.Stfld, idField);
            ilc.Emit(OpCodes.Ret);

            // void Intercept(IInvocation inv) { inv.ReturnValue = _id; }
            var interceptMI = typeof(IInterceptor).GetMethod("Intercept")!;
            var invocationType = typeof(Castle.DynamicProxy.IInvocation);
            var setRV = invocationType.GetProperty("ReturnValue")!.GetSetMethod()!;

            var mb = tb.DefineMethod(
                interceptMI.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                interceptMI.ReturnType,
                new[] { invocationType });

            var il = mb.GetILGenerator();
            // inv.ReturnValue = _id;
            il.Emit(OpCodes.Ldarg_1);       // load inv
            il.Emit(OpCodes.Ldarg_0);       // load this
            il.Emit(OpCodes.Ldfld, idField);// load _id
            il.Emit(OpCodes.Callvirt, setRV);
            il.Emit(OpCodes.Ret);

            tb.DefineMethodOverride(mb, interceptMI);

            return tb.CreateTypeInfo()!;
        }

        var interceptorType = BuildStringReturnInterceptorType();
        var proxyGen = new ProxyGenerator();

        object CreateServiceImpl(Type iface, string id)
        {
            var interceptor = (IInterceptor)Activator.CreateInstance(interceptorType, new object[] { id })!;
            return proxyGen.CreateInterfaceProxyWithoutTarget(iface, interceptor);
        }

        // ---------- Build a small dependency graph for the test ----------
        // Example: we will create N plugins, each provides one service interface.
        // For deterministic but nontrivial dependencies, we make a DAG by only allowing edges from higher index to lower index,
        // then shuffle insertion order to simulate "wrong" insertion.
        var rnd = new Random(12345);
        var pluginCount = 10;

        var serviceTypes = Enumerable.Range(0, pluginCount)
            .Select(i => CreateServiceInterface($"IService_{i}_{Guid.NewGuid():N}"))
            .ToArray();

        // Build dependencies: for each plugin i, choose some dependencies among indices < i (so it's acyclic)
        var dependencies = new Dictionary<int, Type[]>(); // pluginIndex -> required service types
        for (int i = 0; i < pluginCount; i++)
        {
            var possible = Enumerable.Range(0, i).ToArray(); // only prior indices
            var take = possible.Length == 0 ? 0 : rnd.Next(0, Math.Min(3, possible.Length) + 1);
            var deps = possible.OrderBy(_ => rnd.Next()).Take(take).Select(idx => serviceTypes[idx]).ToArray();
            dependencies[i] = deps;
        }

        // We'll create plugin mocks that:
        // - On ConfigureServices: register their provided service (via Type overload)
        // - On StartAsync: resolve their dependencies; record "start" position
        // We'll record call sequence indices for ConfigureServices and StartAsync.
        var configureSequence = new List<string>();
        var startSequence = new List<string>();

        var plugins = new List<(int Index, Mock<IPlugin> Mock)>();

        for (int i = 0; i < pluginCount; i++)
        {
            var idx = i;
            var myServiceType = serviceTypes[idx];
            var required = dependencies[idx];

            var pluginMock = new Mock<IPlugin>();

            pluginMock
                .Setup(p => p.ConfigureServices(It.IsAny<IServiceRegistrar>()))
                .Callback<IServiceRegistrar>(registrar =>
                {
                    // record configure order
                    lock (configureSequence) { configureSequence.Add($"Plugin{idx}.Configure"); }

                    // register this plugin's service into registrar using the Type-based overload
                    registrar.RegisterFactory(
                        myServiceType,
                        _ => CreateServiceImpl(myServiceType, $"impl-{idx}"),
                        ServiceLifetime.Singleton);
                })
                .Verifiable();

            pluginMock
                .Setup(p => p.StartAsync(It.IsAny<IServiceResolver>(), It.IsAny<CancellationToken>()))
                .Callback<IServiceResolver, CancellationToken>((resolver, ct) =>
                {
                    // record start invocation order
                    lock (startSequence) { startSequence.Add($"Plugin{idx}.Start"); }

                    // try to resolve all required services immediately; must be non-null and usable
                    foreach (var dep in required)
                    {
                        var svc = resolver.Resolve(dep);
                        if (svc == null) throw new Exception($"Plugin{idx} failed to resolve {dep.Name} at StartAsync.");
                        // sanity check: GetId returns impl-<providerIndex>
                        var getId = dep.GetMethod("GetId")!;
                        var id = (string)getId.Invoke(svc, null)!;
                        if (string.IsNullOrEmpty(id)) throw new Exception($"Plugin{idx} resolved service {dep.Name} but GetId returned null/empty.");
                    }
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            plugins.Add((idx, pluginMock));
        }

        // ---------- Shuffle insertion order so some consumers come before providers ----------
        var insertionOrder = plugins.OrderBy(_ => rnd.Next()).ToArray();
        var pluginObjects = insertionOrder.Select(p => p.Mock.Object).ToArray();

        // ---------- Insert into container (the host should self-adjust load order) ----------
        // This call is expected to orchestrate ConfigureServices + StartAsync in some internal order that satisfies dependencies.
        pluginContainer.Insert(pluginObjects);

        // ---------- Verifications ----------
        // 1) all plugins configured & started exactly once
        foreach (var (_, mock) in plugins)
        {
            mock.Verify(p => p.ConfigureServices(It.IsAny<IServiceRegistrar>()), Times.Once);
            mock.Verify(p => p.StartAsync(It.IsAny<IServiceResolver>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // 2) For every dependency edge (consumer idx -> provider index), assert the provider's Configure ran before the consumer's Start.
        //    We recorded configureSequence entries like "PluginX.Configure" and startSequence like "PluginY.Start".
        //    We'll map provider configure positions and consumer start positions and assert ordering.
        var configureIndex = new Dictionary<string, int>();
        for (int i = 0; i < configureSequence.Count; i++) configureIndex[configureSequence[i]] = i;

        var startIndex = new Dictionary<string, int>();
        for (int i = 0; i < startSequence.Count; i++) startIndex[startSequence[i]] = i;

        foreach (var consumerIndex in dependencies.Keys)
        {
            var reqs = dependencies[consumerIndex];
            foreach (var req in reqs)
            {
                // find provider index via serviceTypes array
                var providerIndex = Array.IndexOf(serviceTypes, req);
                var providerKey = $"Plugin{providerIndex}.Configure";
                var consumerStartKey = $"Plugin{consumerIndex}.Start";

                // ensure keys exist
                if (!configureIndex.TryGetValue(providerKey, out var providerPos))
                    throw new Exception($"Provider configure entry missing for {providerKey}");
                if (!startIndex.TryGetValue(consumerStartKey, out var consumerPos))
                    throw new Exception($"Consumer start entry missing for {consumerStartKey}");

                if (!(providerPos < consumerPos))
                {
                    // failure: provider's Configure did not run before consumer Start
                    throw new Exception($"Load order not adjusted: provider Plugin{providerIndex} configured at {providerPos}, but consumer Plugin{consumerIndex} started at {consumerPos}.");
                }
            }
        }

        // If we reach here, the host correctly adjusted load order for all edges.
    }

    public void Insert_MustRejectServicesNotInExportedContracts(IPluginContainer container)
    {
        // Arrange
        var pluginMock = new Mock<IPlugin>();
        var invalidType = typeof(string); // not in ExportedServiceContracts

        pluginMock.Setup(p => p.ExportedServiceContracts)
                  .Returns(new[] { typeof(IDisposable) });

        pluginMock.Setup(p => p.ConfigureServices(It.IsAny<IServiceRegistrar>()))
                  .Callback<IServiceRegistrar>(reg =>
                  {
                      // tries to register a type not declared in ExportedServiceContracts
                      reg.RegisterFactory(invalidType, _ => "oops", ServiceLifetime.Singleton);
                  });

        Assert.Throws<InvalidOperationException>(() =>
            container.Insert(new[] { pluginMock.Object }));
    }
}