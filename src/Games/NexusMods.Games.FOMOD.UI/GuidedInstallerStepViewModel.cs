using System.Reactive;
using System.Reactive.Linq;
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
    public ChoiceGroup<int, int>[] AvailableChoices { get; set; } = Array.Empty<ChoiceGroup<int, int>>();

    [Reactive]
    public TaskCompletionSource<KeyValuePair<int, int[]>[]?>? TaskCompletionSource { get; set; }

    public ReactiveCommand<Unit, Unit> NextStepCommand { get; }

    public GuidedInstallerStepViewModel()
    {
        // TODO: other validation
        var hasTaskCompletionSource = this
            .WhenAnyValue(vm => vm.TaskCompletionSource)
            .OnUI()
            .Select(tcs => tcs is not null);

        NextStepCommand = ReactiveCommand.Create(() =>
        {
            // TODO: set result
            TaskCompletionSource?.TrySetResult(Array.Empty<KeyValuePair<int, int[]>>());
        }, hasTaskCompletionSource);
    }
}
