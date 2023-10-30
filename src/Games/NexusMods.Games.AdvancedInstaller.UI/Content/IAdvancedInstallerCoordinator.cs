using System.Reactive.Subjects;
using DynamicData.Binding;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;

using ISelectableDirectoryTreeEntryVM = NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry.ITreeEntryViewModel;
namespace NexusMods.Games.AdvancedInstaller.UI.Content;


/// <summary>
///     Interface for a component which facilitates the exchange of information between different AdvancedInstaller components.
/// </summary>
public interface IAdvancedInstallerCoordinator
{
    /// <summary>
    ///     This observable should be notified when an archive entry is selected from the Mod Content section.
    /// </summary>
    public ISubject<ITreeEntryViewModel> StartSelectObserver { get; }

    /// <summary>
    ///     This observable should be notified when user cancels the selection of an archive entry from the Mod Content section.
    /// </summary>
    public ISubject<ITreeEntryViewModel> CancelSelectObserver { get; }

    /// <summary>
    ///     This observable should be notified when user unlinks a file binding, either from the Mod Content section or the Preview section.
    /// </summary>
    public ISubject<ISelectableDirectoryTreeEntryVM> DirectorySelectedObserver { get; }

    /// <summary>
    ///     Retrieves the deployment data used.
    /// </summary>
    public DeploymentData Data { get; }
}

public class DummyCoordinator : IAdvancedInstallerCoordinator
{
    public ISubject<ITreeEntryViewModel> StartSelectObserver { get; } = new Subject<ITreeEntryViewModel>();
    public ISubject<ITreeEntryViewModel> CancelSelectObserver { get; } = new Subject<ITreeEntryViewModel>();
    public ISubject<ISelectableDirectoryTreeEntryVM> DirectorySelectedObserver { get; } = new Subject<ISelectableDirectoryTreeEntryVM>();
    public DeploymentData Data { get; } = new();
}

