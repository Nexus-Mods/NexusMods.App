using System.Reactive;
using System.Reactive.Disposables;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

internal class SuggestedEntryViewModel : AViewModel<ISuggestedEntryViewModel>,
    ISuggestedEntryViewModel
{
    public SuggestedEntryViewModel(GameLocationsRegister register, LocationId locationId, string? subtitle,
        IAdvancedInstallerCoordinator coordinator, ISelectableTreeEntryViewModel correspondingNode)
    {
        Title = register[locationId].FileName;
        Subtitle = subtitle ?? locationId.Value;
        CorrespondingNode = correspondingNode;

        this.WhenActivated(disposables =>
        {
            SelectCommand = ReactiveCommand.Create(CorrespondingNode.Link).DisposeWith(disposables);
        });
    }

    public string Title { get; }
    public string Subtitle { get; }
    public ISelectableTreeEntryViewModel CorrespondingNode { get; }
    public ReactiveCommand<Unit, Unit> SelectCommand { get; private set; } = Initializers.EnabledReactiveCommand;
}
