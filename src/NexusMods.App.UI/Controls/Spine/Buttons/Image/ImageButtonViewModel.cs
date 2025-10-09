using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Media;
using NexusMods.App.UI.Controls.LoadoutBadge;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Image;

public class ImageButtonViewModel : AViewModel<IImageButtonViewModel>, IImageButtonViewModel
{
    [Reactive]
    public bool IsActive { get; set; }

    [Reactive] public string Name { get; set; } = "";

    [Reactive] public IImage Image { get; set; } = Initializers.IImage;

    [Reactive] public ReactiveCommand<Unit,Unit> Click { get; set; } = Initializers.EmptyReactiveCommand;
    
    public IWorkspaceContext? WorkspaceContext { get; set; }
    
    public ILoadoutBadgeViewModel? LoadoutBadgeViewModel { get; set; }
    
    public ImageButtonViewModel()
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.IsActive)
                .SubscribeWithErrorLogging(isActive =>
                {
                    if (LoadoutBadgeViewModel != null)
                    {
                        LoadoutBadgeViewModel.IsLoadoutSelected = isActive;
                    }
                })
                .DisposeWith(d);
        });
    }
}
