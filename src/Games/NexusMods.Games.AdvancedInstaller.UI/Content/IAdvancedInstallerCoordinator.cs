using System.Reactive.Subjects;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

namespace NexusMods.Games.AdvancedInstaller.UI;


/// <summary>
///     Interface for a component which facilitates the exchange of information between different AdvancedInstaller components.
/// </summary>
public interface IAdvancedInstallerCoordinator
{
    /// <summary>
    ///     This observable should be notified when an archive entry is selected from the Mod Content section.
    /// </summary>
    public Subject<IModContentTreeEntryViewModel> StartSelectObserver { get; }

    /// <summary>
    ///     This observable should be notified when user cancels the selection of an archive entry from the Mod Content section.
    /// </summary>
    public Subject<IModContentTreeEntryViewModel> CancelSelectObserver { get; }

    /// <summary>
    ///     This observable should be notified when user unlinks a file binding, either from the Mod Content section or the Preview section.
    /// </summary>
    public Subject<ISelectableTreeEntryViewModel> DirectorySelectedObserver { get; }

    /// <summary>
    ///     Retrieves the deployment data used.
    /// </summary>
    public DeploymentData Data { get; }
}

public class DummyCoordinator : IAdvancedInstallerCoordinator
{
    public Subject<IModContentTreeEntryViewModel> StartSelectObserver { get; } = new();
    public Subject<IModContentTreeEntryViewModel> CancelSelectObserver { get; } = new();
    public Subject<ISelectableTreeEntryViewModel> DirectorySelectedObserver { get; } = new();
    public DeploymentData Data { get; } = new();
}

