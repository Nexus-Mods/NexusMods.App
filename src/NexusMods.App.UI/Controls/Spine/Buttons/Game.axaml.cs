using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Spine.Buttons;

public partial class Game : ReactiveUserControl<GameViewModel>
{
    public Image? _image;

    public Game()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            _image = this.FindDescendantOfType<Image>();
            this.OneWayBind(ViewModel, vm => vm.Image, v => v._image.Source)
                .DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.IsActive, v => v.Toggle.IsChecked)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.Click, v => v.Toggle.Command)
                .DisposeWith(disposables);
        });
    }

}