using NexusMods.App.UI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI;

public interface ITreeEntryViewModel : IViewModel
{
    #region Left Elements

    [Reactive] public bool ShowCreateFolderButton => false;

    [Reactive] public bool ShowFileIcon => false;

    [Reactive] public bool ShowFolderIcon => false;

    [Reactive] public bool ShowFileName => false;

    [Reactive] public string FileName => string.Empty;

    [Reactive] public bool ShowCreateFolderNameTextBox => false;

    [Reactive] public string CreateFolderName => string.Empty;

    [Reactive] public bool ShowNewPill => false;

    [Reactive] public bool ShowDupeFolderPill => false;

    [Reactive] public bool ShowFolderMergedPill => false;

    #endregion

    #region Right Elements

    [Reactive] public bool ShowInstallButton => false;

    [Reactive] public string InstallButtonText => string.Empty;

    [Reactive] public bool ShowXSelectLocationButton => false;

    [Reactive] public bool ShowXButton => false;

    [Reactive] public bool ShowRemoveFromLocationButton => false;

    [Reactive] public string RemoveFromLocationButtonText => string.Empty;

    [Reactive] public bool ShowIncludeButton => false;

    [Reactive] public string IncludeButtonText => string.Empty;

    [Reactive] public bool ShowIncludedRemoveButton => false;

    [Reactive] public string IncludedRemoveButtonText => string.Empty;

    [Reactive] public bool ShowSelectButton => false;

    [Reactive] public bool ShowDeleteCreatedFolderButton => false;

    [Reactive] public bool ShowCancelCreateFolderButton => false;

    [Reactive] public bool ShowSaveCreatedFolderButton => false;

    #endregion
}
