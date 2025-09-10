using Domain.DI;
using System;
using Xunit;

namespace Domain.Tests.DI;

public abstract partial class DITestBase
{
    /// <summary>
    /// Must be implemented by derived test classes to provide a container implementation.
    /// </summary>
    protected abstract IServiceContainer CreateContainer();

    [Fact]
    public void RegisterFactory_ShouldThrow_IfServiceIsNotInterface()
    {
        var container = CreateContainer();

        Assert.Throws<ArgumentException>(() =>
            container.RegisterFactory(
                _ => "NotAnInterface",
                ServiceLifetime.Singleton));
    }

    [Fact]
    public void Singleton_ShouldReturnSameInstance()
    {
        var container = CreateContainer();
        container.RegisterFactory<IFoo>(_ => new Foo(), ServiceLifetime.Singleton);

        var a = container.Resolve<IFoo>()!;
        var b = container.Resolve<IFoo>()!;

        Assert.Same(a, b);
    }

    [Fact]
    public void Transient_ShouldReturnDifferentInstances()
    {
        var container = CreateContainer();
        container.RegisterFactory<IFoo>(_ => new Foo(), ServiceLifetime.Transient);

        var a = container.Resolve<IFoo>()!;
        var b = container.Resolve<IFoo>()!;

        Assert.NotSame(a, b);
    }

    [Fact]
    public void Scoped_ShouldReturnSameInstanceWithinScope()
    {
        var container = CreateContainer();
        container.RegisterFactory<IFoo>(_ => new Foo(), ServiceLifetime.Scoped);

        using var scope = container.CreateScope();
        var resolver = scope.ServiceResolver;

        var a = resolver.Resolve<IFoo>()!;
        var b = resolver.Resolve<IFoo>()!;

        Assert.Same(a, b);
    }

    [Fact]
    public void Scoped_ShouldReturnDifferentInstancesAcrossScopes()
    {
        var container = CreateContainer();
        container.RegisterFactory<IFoo>(_ => new Foo(), ServiceLifetime.Scoped);

        IFoo a;
        using (var scope1 = container.CreateScope())
            a = scope1.ServiceResolver.Resolve<IFoo>()!;

        IFoo b;
        using (var scope2 = container.CreateScope())
            b = scope2.ServiceResolver.Resolve<IFoo>()!;

        Assert.NotSame(a, b);
    }

    [Fact]
    public void Resolve_ByType_ShouldMatchGenericResolve()
    {
        var container = CreateContainer();
        container.RegisterFactory<IFoo>(_ => new Foo(), ServiceLifetime.Singleton);

        var generic = container.Resolve<IFoo>()!;
        var nonGeneric = (IFoo)container.Resolve(typeof(IFoo))!;

        Assert.Same(generic, nonGeneric);
    }

    [Fact]
    public void Resolve_UnregisteredService_ShouldReturnNull()
    {
        var container = CreateContainer();
        var result = container.Resolve<IFoo>();
        Assert.Null(result);
    }

    [Fact]
    public void ScopedService_ShouldBeDisposed_WhenScopeEnds()
    {
        var container = CreateContainer();
        container.RegisterFactory<IBar>(_ => new Bar(), ServiceLifetime.Scoped);

        IBar bar;
        using (var scope = container.CreateScope())
        {
            bar = scope.ServiceResolver.Resolve<IBar>()!;
            Assert.False(bar.Disposed);
        }

        Assert.True(bar.Disposed);
    }
}

// Dummy services for testing
public interface IFoo { Guid Id { get; } }
public class Foo : IFoo { public Guid Id { get; } = Guid.NewGuid(); }

public interface IBar : IDisposable
{
    Guid Id { get; }
    bool Disposed { get; }
}
public class Bar : IBar
{
    public Guid Id { get; } = Guid.NewGuid();
    public bool Disposed { get; private set; }
    public void Dispose() => Disposed = true;
}
