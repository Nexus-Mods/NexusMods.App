using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public partial class ModEnabledView : ReactiveUserControl<IModEnabledViewModel>
{
    public ModEnabledView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.Enabled)
                .BindToUi(this, view => view.EnabledToggleSwitch.IsChecked)
                .DisposeWith(d);
            this.WhenAnyValue(view => view.ViewModel!.ToggleEnabledCommand)
                .BindTo(this, view => view.EnabledToggleSwitch.Command)
                .DisposeWith(d);
        });
    }
}

