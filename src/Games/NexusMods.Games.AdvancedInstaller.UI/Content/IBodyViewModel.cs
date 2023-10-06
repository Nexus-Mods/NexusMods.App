using NexusMods.App.UI;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

namespace NexusMods.Games.AdvancedInstaller.UI.Content;

public interface IBodyViewModel : IViewModel
{
    public IModContentViewModel ModContentViewModel { get; }

    public IPreviewViewModel PreviewViewModel { get; }

    public IEmptyPreviewViewModel EmptyPreviewViewModel { get; }

    public IViewModel CurrentPreviewViewModel { get; }
}
