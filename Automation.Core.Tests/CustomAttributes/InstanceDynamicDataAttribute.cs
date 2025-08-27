using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Automation.Core.Tests.CustomAttributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InstanceDynamicDataAttribute : Attribute, ITestDataSource
{
    private readonly string _methodName;

    public InstanceDynamicDataAttribute(string methodName)
    {
        _methodName = methodName;
    }

    public IEnumerable<object[]> GetData(MethodInfo methodInfo)
    {
        object classInstance = Activator.CreateInstance(methodInfo.DeclaringType!);

        MethodInfo? dataMethod = methodInfo.DeclaringType!
            .GetMethod(_methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (dataMethod == null)
        {
            throw new InvalidOperationException($"Cannot find an instance method named '{_methodName}' on type '{methodInfo.DeclaringType!.FullName}'.");
        }

        var rawData = dataMethod.Invoke(classInstance, null) as IEnumerable<object[]>;
        if (rawData == null)
        {
            throw new InvalidOperationException($"The method '{_methodName}' on type '{methodInfo.DeclaringType!.FullName}' did not return IEnumerable<object[]>.");
        }

        return rawData;
    }

    public string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        return null;
    }
}
