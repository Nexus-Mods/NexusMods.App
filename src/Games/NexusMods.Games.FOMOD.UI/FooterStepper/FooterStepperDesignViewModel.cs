using NexusMods.App.UI;
using NexusMods.DataModel.RateLimiting;

namespace NexusMods.Games.FOMOD.UI;

public class FooterStepperDesignViewModel : FooterStepperViewModel
{
    public FooterStepperDesignViewModel()
    {
        Progress = Percent.CreateClamped(5, 10);
        GoToPrevCommand = Initializers.DisabledReactiveCommand;
        GoToNextCommand = Initializers.EnabledReactiveCommand;
    }
}
