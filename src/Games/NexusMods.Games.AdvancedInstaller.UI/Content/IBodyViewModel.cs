using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

namespace NexusMods.Games.AdvancedInstaller.UI.Content;

public interface IBodyViewModel : IViewModel
{
    public string ModName { get; set; }

    public IModContentViewModel ModContentViewModel { get; }

    public IPreviewViewModel PreviewViewModel { get; }

    public IEmptyPreviewViewModel EmptyPreviewViewModel { get; }
    public ISelectLocationViewModel SelectLocationViewModel { get; }

    public bool CanInstall { get; }

    /// <summary>
    ///     The viewmodel of the item to be shown on the screen.
    ///     Can be either <see cref="PreviewViewModel"/>, <see cref="SelectLocationViewModel"/> or <see cref="EmptyPreviewViewModel"/>.
    /// </summary>
    public IViewModel CurrentPreviewViewModel { get; }

    /// <summary>
    ///     Stores the data used for deployment.
    /// </summary>
    public DeploymentData Data { get; }
}
