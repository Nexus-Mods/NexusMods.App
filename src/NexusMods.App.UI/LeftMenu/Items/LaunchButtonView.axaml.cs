using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public partial class LaunchButtonView : ReactiveUserControl<ILaunchButtonViewModel>
{
    public LaunchButtonView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.BindCommand(ViewModel, vm => vm.Command, v => v.LaunchButton)
                .DisposeWith(d);

            this.Bind(ViewModel, vm => vm.Label, v => v.LaunchText.Text)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Command)
                .SelectMany(cmd => cmd.CanExecute)
                .Select(canExecute =>
                    canExecute ? IconType.Play : IconType.HourglassEmpty)
                .Select(icon => icon.ToMaterialUiName())
                .BindTo(this, view => view.LaunchIcon.Value)
                .DisposeWith(d);
        });
    }
}

