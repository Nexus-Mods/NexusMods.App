using NexusMods.App.UI.Controls.MiniGameWidget.Standard;
using ReactiveUI;
using System.Reactive;
using NexusMods.UI.Sdk;

namespace NexusMods.App.UI.Controls.MiniGameWidget.ComingSoon;

public class ComingSoonMiniGameWidgetViewModelDesignViewModel : AViewModel<IComingSoonMiniGameWidgetViewModel>, IComingSoonMiniGameWidgetViewModel
{
    public static MiniGameWidgetDesignViewModel Instance { get; } = new();

    public ReactiveCommand<Unit, Unit> ViewRoadmapCommand { get; } = ReactiveCommand.CreateFromTask(() => Task.CompletedTask);
}
