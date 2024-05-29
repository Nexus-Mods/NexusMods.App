using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public static class ServiceExtensions
{
    public static IServiceCollection AddUISettings(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSettings<LoadoutGridSettings>()
            .AddSettings<LanguageSettings>()
            .AddSettings<TextEditorSettings>()
            .AddSettings<AlphaSettings>()
            .AddSettings<LoginSettings>();
    }
}
