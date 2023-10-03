using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public static class Services
{
    public static IServiceCollection AddAdvancedInstallerUi(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddViewModel<AdvancedInstallerOverlayViewModel, IAdvancedInstallerOverlayViewModel>()

            .AddView<AdvancedInstallerFooterView, IAdvancedInstallerFooterViewModel>()
            .AddView<AdvancedInstallerBodyView, IAdvancedInstallerBodyViewModel>()
            .AddView<AdvancedInstallerModContentView, IAdvancedInstallerModContentViewModel>()
            .AddView<AdvancedInstallerResultsView, IAdvancedInstallerResultsViewModel>()
            .AddView<AdvancedInstallerOverlayView, IAdvancedInstallerOverlayViewModel>()
            .AddView<AdvancedInstallerTreeEntryView, IAdvancedInstallerTreeEntryViewModel>();

    }
}
