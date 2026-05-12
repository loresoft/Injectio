using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using XUnit.Hosting.Logging;

namespace Injectio.Acceptance.Tests;

public class DependencyInjectionFixture : TestApplicationFixture
{
    protected override void ConfigureApplication(HostApplicationBuilder builder)
    {
        base.ConfigureApplication(builder);

        builder.Logging.AddMemoryLogger();

        var services = builder.Services;

        services.AddInjectioTestsLibrary("Alpha", "Beta");
        services.AddInjectioAcceptanceTests("Alpha", "Beta");
    }
}
