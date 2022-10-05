namespace Injectio.Generators;

public class ServiceRegistration
{
    public string Factory { get; set; }

    public string ImplementationType { get; set; }

    public HashSet<string> ServiceTypes { get; set; } = new HashSet<string>();

    public string Lifetime { get; set; } = "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient";

    public object Duplicate { get; set; }

    public object Registration { get; set; }
}
