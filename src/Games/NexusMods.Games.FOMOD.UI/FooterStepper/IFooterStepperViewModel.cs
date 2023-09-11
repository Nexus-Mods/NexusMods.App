using System.Reactive;
using NexusMods.App.UI;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public interface IFooterStepperViewModel : IViewModel
{
    public bool IsLastStep { get; set; }

    public Percent Progress { get; set; }

    public ReactiveCommand<Unit, Unit> GoToNextCommand { get; set; }

    public ReactiveCommand<Unit, Unit> GoToPrevCommand { get; set; }
}
