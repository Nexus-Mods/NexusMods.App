using Microsoft.Extensions.DependencyInjection;
using NexusMods.Games.AdvancedInstaller.UI.Content;
using NexusMods.Games.AdvancedInstaller.UI.Content.Bottom;
using NexusMods.Games.AdvancedInstaller.UI.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Games.AdvancedInstaller.UI.Preview;
using NexusMods.Games.AdvancedInstaller.UI.SelectLocation;
using ModContentView = NexusMods.Games.AdvancedInstaller.UI.ModContent.ModContentView;

namespace NexusMods.Games.AdvancedInstaller.UI;

public static class Services
{
    public static IServiceCollection AddAdvancedInstallerUi(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<IAdvancedInstallerHandler, AdvancedInstallerHandlerUI>()
            .AddViewModel<AdvancedInstallerOverlayViewModel, IAdvancedInstallerOverlayViewModel>()
            .AddView<FooterView, IFooterViewModel>()
            .AddView<BodyView, IBodyViewModel>()
            .AddView<ModContentView, IModContentViewModel>()
            .AddView<PreviewView, IPreviewViewModel>()
            .AddView<LocationPreviewTreeView, ILocationPreviewTreeViewModel>()
            .AddView<EmptyPreviewView, IEmptyPreviewViewModel>()
            .AddView<SelectLocationView, ISelectLocationViewModel>()
            .AddView<SelectLocationTreeView, ISelectLocationTreeViewModel>()
            .AddView<SuggestedEntryView, ISuggestedEntryViewModel>()
            .AddView<AdvancedInstallerOverlayView, IAdvancedInstallerOverlayViewModel>()
            .AddView<ModContentTreeEntryView, IModContentTreeEntryViewModel>()
            .AddView<PreviewTreeEntryView, IPreviewTreeEntryViewModel>()
            .AddView<SelectableTreeEntryView, ISelectableTreeEntryViewModel>()
            .AddView<UnsupportedModOverlayView, IUnsupportedModOverlayViewModel>();
    }
}
