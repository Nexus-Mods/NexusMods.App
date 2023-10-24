using System.Diagnostics.CodeAnalysis;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

namespace NexusMods.Games.AdvancedInstaller.UI.Content;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
internal class BodyDesignViewModel : AViewModel<IBodyViewModel>,
    IBodyViewModel
{
    public IModContentViewModel ModContentViewModel { get; } = new ModContentDesignViewModel();
    public IPreviewViewModel PreviewViewModel { get; } = new PreviewDesignViewModel();

    public IEmptyPreviewViewModel EmptyPreviewViewModel { get; } =
        new EmptyPreviewDesignViewModel();

    public ISelectLocationViewModel SelectLocationViewModel { get; } = new SelectLocationDesignViewModel();

    public IViewModel CurrentPreviewViewModel => PreviewViewModel;
    public DeploymentData Data { get; } = new();
}
