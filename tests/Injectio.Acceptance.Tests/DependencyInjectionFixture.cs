using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using XUnit.Hosting;

namespace Injectio.Acceptance.Tests;

public class DependencyInjectionFixture : TestApplicationFixture
{
    protected override void ConfigureApplication(HostApplicationBuilder builder)
    {
        base.ConfigureApplication(builder);

        var services = builder.Services;

        services.AddInjectioTestsLibrary("Alpha", "Beta");
        services.AddInjectioAcceptanceTests("Alpha", "Beta");
    }
}
