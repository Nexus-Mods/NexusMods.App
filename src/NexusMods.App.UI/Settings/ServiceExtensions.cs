using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public static class ServiceExtensions
{
    public static IServiceCollection AddUISettings(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSettings<LanguageSettings>()
            .AddSettings<TextEditorSettings>()
            .AddSettings<AlertSettings>()
            .AddSettings<BehaviorSettings>()
            .AddSettings<UpdaterSettings>()
            .AddSettings<WelcomeSettings>()
            .AddSettings<DiscordSettings>();
    }
}
