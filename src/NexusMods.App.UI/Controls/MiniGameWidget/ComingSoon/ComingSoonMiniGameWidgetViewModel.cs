using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.CrossPlatform.Process;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


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
