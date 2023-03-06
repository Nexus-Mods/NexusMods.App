using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using Noggog;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Image;

public partial class ImageButton : ReactiveUserControl<IImageButtonViewModel>
{
    private Avalonia.Controls.Image? _image;

    public ImageButton()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.ViewModel.IsActive)
                .StartWith(false)
                .Subscribe(SetClasses)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.Click, v => v.Button)
                .DisposeWith(d);

            _image = this.FindDescendantOfType<Avalonia.Controls.Image>();
            this.OneWayBind(ViewModel, vm => vm.Image, v => v._image!.Source)
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
