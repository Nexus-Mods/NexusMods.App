using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public static class Services
{
    public static IServiceCollection AddAdvancedInstallerUi(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddView<AdvancedInstallerBodyView, IAdvancedInstallerBodyViewModel>();
    }
}
