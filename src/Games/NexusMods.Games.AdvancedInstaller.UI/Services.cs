using Microsoft.Extensions.DependencyInjection;
using NexusMods.Games.AdvancedInstaller.UI.Content;
using NexusMods.Games.AdvancedInstaller.UI.Content.Bottom;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;
using TreeEntryView = NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry.TreeEntryView;

namespace NexusMods.Games.AdvancedInstaller.UI;

public static class Services
{
    public static IServiceCollection AddAdvancedInstallerUi(this IServiceCollection serviceCollection)
    {
        return serviceCollection
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
            .AddView<Content.Left.TreeEntryView, Content.Left.ITreeEntryViewModel>()
            .AddView<TreeEntryView, Content.Right.Results.PreviewView.PreviewEntry.ITreeEntryViewModel>()
            .AddView<Content.Right.Results.SelectLocation.SelectableDirectoryEntry.TreeEntryView,
                Content.Right.Results.SelectLocation.SelectableDirectoryEntry.ITreeEntryViewModel>()
            .AddView<UnsupportedModOverlayView, IUnsupportedModOverlayViewModel>();
    }
}
