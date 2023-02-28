using Microsoft.Extensions.Logging;
using NexusMods.App.UI.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.TopBar;

public class TopBarViewModel : AViewModel
{
    //private readonly ILogger<TopBarViewModel> _logger;

    public TopBarViewModel(ILogger<TopBarViewModel> logger)
    {
    }

    [Reactive] public string? Title { get; set; } = "The title goes here";

}