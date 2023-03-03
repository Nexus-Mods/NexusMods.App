
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.Controls.Spine.Buttons;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;
using NexusMods.App.UI.Controls.TopBar;
using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.ViewModels;
using NexusMods.App.UI.Views;
using ImageButton = NexusMods.App.UI.Controls.Spine.Buttons.Image.ImageButton;

namespace NexusMods.App.UI;

public static class Services
{
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddUI(this IServiceCollection c)
    {
        return c.AddTransient<MainWindow>()
            // View Models
            .AddTransient<MainWindowViewModel>()
            .AddTransient<FoundGamesViewModel>()

            .AddViewModel<IconButtonViewModel, IIconButtonViewModel>()
            .AddViewModel<SpineViewModel, ISpineViewModel>()
            .AddViewModel<TopBarViewModel, ITopBarViewModel>()

            // Views
            .AddView<IconButton, IIconButtonViewModel>()
            .AddView<Spine, ISpineViewModel>()
            .AddView<FoundGamesView, FoundGamesViewModel>()
            .AddView<ImageButton, IImageButtonViewModel>()
            .AddView<TopBarView, ITopBarViewModel>()

            // Other
            .AddSingleton<InjectedViewLocator>()
            .AddSingleton<App>();
    }

}
