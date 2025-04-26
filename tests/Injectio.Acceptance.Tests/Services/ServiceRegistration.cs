using System.Collections.Concurrent;

using Injectio.Attributes;

using Microsoft.Extensions.DependencyInjection;

namespace Injectio.Acceptance.Tests.Services;

public static class ServiceRegistration
{
    public static ConcurrentDictionary<string, IEnumerable<string>?> Tags { get; } = new();

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
        Tags[nameof(RegisterServices)] = [];
    }
}
