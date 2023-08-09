using System.Reactive;
using NexusMods.App.UI;
using NexusMods.Common.UserInput;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public interface IGuidedInstallerStepViewModel : IViewModelInterface
{
    public ChoiceGroup<int, int>[] Choices { get; set; }

    public TaskCompletionSource<Tuple<int, int[]>?>? TaskCompletionSource { get; set; }

    public ReactiveCommand<Unit, Unit> NextStepCommand { get; }
}
