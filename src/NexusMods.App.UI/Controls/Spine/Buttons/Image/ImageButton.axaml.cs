using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Image;

public partial class ImageButton : ReactiveUserControl<IImageButtonViewModel>
{
    public ImageButton()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.ViewModel!.IsActive)
                .StartWith(false)
                .SubscribeWithErrorLogging(SetClasses)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.Click, v => v.Button)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.Image, view => view.Image.Value, image => new IconValue(new AvaloniaImage(image)))
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.Name, v => v.ToolTipTextBlock.Text)
                .DisposeWith(d);
            
            this.OneWayBind(ViewModel, vm => vm.LoadoutBadgeViewModel, v => v.LoadoutBadge.ViewModel)
                .DisposeWith(d);
        });
    }

    private void SetClasses(bool isActive)
    {
        if (isActive)
        {
            Button.Classes.Add("Active");
            Button.Classes.Remove("Inactive");
        }
        else
        {
            Button.Classes.Remove("Active");
            Button.Classes.Add("Inactive");
        }
    }

}
