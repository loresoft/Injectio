using System.Collections.Concurrent;

using Injectio.Attributes;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Injectio.Acceptance.Tests.Services;

public static class ServiceRegistration
{
    private static int _hostInvocationCount;
    private static int _serviceInvocationCount;

    public static ConcurrentDictionary<string, IEnumerable<string>?> Tags { get; } = new();

    public static int HostInvocationCount => Volatile.Read(ref _hostInvocationCount);

    public static int ServiceInvocationCount => Volatile.Read(ref _serviceInvocationCount);

    [RegisterServices]
    public static void RegisterWithTagsSet(IServiceCollection services, ISet<string> tags)
    {
        Tags[nameof(RegisterWithTagsSet)] = tags;
    }

    [RegisterServices]
    public static void RegisterWithTagsReadOnly(IServiceCollection services, IReadOnlySet<string> tags)
    {
        Tags[nameof(RegisterWithTagsReadOnly)] = tags;
    }

    [RegisterServices]
    public static void RegisterWithTagsCollection(IServiceCollection services, ICollection<string> tags)
    {
        Tags[nameof(RegisterWithTagsCollection)] = tags;
    }

    [RegisterServices]
    public static void RegisterServices(IServiceCollection services)
    {
        Interlocked.Increment(ref _serviceInvocationCount);
        Tags[nameof(RegisterServices)] = [];
    }

    [RegisterServices]
    public static void RegisterHostServices(IHostApplicationBuilder builder)
    {
        Interlocked.Increment(ref _hostInvocationCount);
    }
}
