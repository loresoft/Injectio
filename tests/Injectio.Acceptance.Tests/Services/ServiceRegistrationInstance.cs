using Injectio.Attributes;

using Microsoft.Extensions.DependencyInjection;

namespace Injectio.Acceptance.Tests.Services;

public class ServiceRegistrationInstance
{
    [RegisterServices]
    public void RegisterWithTagsSet(IServiceCollection services, ISet<string> tags)
    {

    }

    [RegisterServices]
    public void RegisterWithTagsReadOnly(IServiceCollection services, IReadOnlySet<string> tags)
    {

    }

    [RegisterServices]
    public void RegisterWithTagsCollection(IServiceCollection services, ICollection<string> tags)
    {

    }

    [RegisterServices]
    public void RegisterServices(IServiceCollection services)
    {

    }
}
