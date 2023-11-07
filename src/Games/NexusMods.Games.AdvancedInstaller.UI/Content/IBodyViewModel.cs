using NexusMods.Games.AdvancedInstaller.UI.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Games.AdvancedInstaller.UI.Preview;
using NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

namespace NexusMods.Games.AdvancedInstaller.UI;

public interface IBodyViewModel : IViewModelInterface
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
    public IViewModelInterface CurrentPreviewViewModel { get; }

    /// <summary>
    ///     Stores the data used for deployment.
    /// </summary>
    public DeploymentData Data { get; }
}
