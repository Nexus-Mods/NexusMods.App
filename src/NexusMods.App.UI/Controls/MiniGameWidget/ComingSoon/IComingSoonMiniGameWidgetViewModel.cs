using NexusMods.Abstractions.UI;
using ReactiveUI;
using System.Reactive;

namespace NexusMods.App.UI.Controls.MiniGameWidget.ComingSoon;

public interface IComingSoonMiniGameWidgetViewModel : IViewModelInterface
{
    ReactiveCommand<Unit, Unit> ViewRoadmapCommand { get; }
}
