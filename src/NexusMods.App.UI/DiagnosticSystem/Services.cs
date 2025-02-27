using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;

namespace NexusMods.App.UI.DiagnosticSystem;

public static class Services
{
    public static IServiceCollection AddDiagnosticWriter(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<IValueFormatter, LoadoutReferenceFormatter>()
            .AddSingleton<IValueFormatter, NamedLinkFormatter>()
            .AddSingleton<IValueFormatter, LoadoutItemGroupFormatter>()
            .AddSingleton<IDiagnosticWriter, DiagnosticWriter>();
    }
}
