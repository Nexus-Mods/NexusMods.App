using System.Reactive;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.CrossPlatform.Process;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.MiniGameWidget.ComingSoon;

public class ComingSoonMiniGameWidgetViewModel : AViewModel<IComingSoonMiniGameWidgetViewModel>, IComingSoonMiniGameWidgetViewModel
{
    private readonly ILogger<ComingSoonMiniGameWidgetViewModel> _logger;
    private const string ViewRoadmapUrl = "https://trello.com/b/gPzMuIr3/nexus-mods-app-roadmap";

    public ComingSoonMiniGameWidgetViewModel(ILogger<ComingSoonMiniGameWidgetViewModel> logger, 
        IOSInterop osInterop,
        ISettingsManager settingsManager)
    {
        _logger = logger;

        ViewRoadmapCommand = ReactiveCommand.CreateFromTask(async () => { await osInterop.OpenUrl(new Uri(ViewRoadmapUrl)); });
    }

    public ReactiveCommand<Unit, Unit> ViewRoadmapCommand { get; }

}
