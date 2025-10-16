using ReactiveUI;
using System.Reactive;
using NexusMods.UI.Sdk;

namespace NexusMods.App.UI.Controls.MiniGameWidget.ComingSoon;

public interface IComingSoonMiniGameWidgetViewModel : IViewModelInterface
{
    ReactiveCommand<Unit, Unit> ViewRoadmapCommand { get; }
}
