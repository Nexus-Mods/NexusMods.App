using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using JetBrains.Annotations;
using NexusMods.App.UI;
using NexusMods.Common.UserInput;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

[UsedImplicitly]
public class GuidedInstallerStepViewModel : AViewModel<IGuidedInstallerStepViewModel>, IGuidedInstallerStepViewModel
{
    [Reactive]
    public ChoiceGroup<int, int>[] Choices { get; set; } = Array.Empty<ChoiceGroup<int, int>>();

    [Reactive]
    public TaskCompletionSource<Tuple<int, int[]>?>? TaskCompletionSource { get; set; }

    public ReactiveCommand<Unit, Unit> NextStepCommand { get; }

    public GuidedInstallerStepViewModel()
    {
        // TODO: other validation
        var hasTaskCompletionSource = this
            .WhenAnyValue(vm => vm.TaskCompletionSource)
            .OnUI()
            .Select(tsc => tsc is not null);

        NextStepCommand = ReactiveCommand.Create(() =>
        {
            // TODO: set result
            TaskCompletionSource?.TrySetResult(new Tuple<int, int[]>(0, Array.Empty<int>()));
        }, hasTaskCompletionSource);
    }
}
