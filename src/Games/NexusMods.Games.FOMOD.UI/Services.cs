using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;

namespace NexusMods.Games.FOMOD.UI;

public static class Services
{
    public static IServiceCollection AddFomodInstallerUi(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddTransient<IGuidedInstaller, GuidedInstallerUi>()
            .AddViewModel<GuidedInstallerStepViewModel, IGuidedInstallerStepViewModel>()
            .AddViewModel<GuidedInstallerWindowViewModel, IGuidedInstallerWindowViewModel>();
    }
}
