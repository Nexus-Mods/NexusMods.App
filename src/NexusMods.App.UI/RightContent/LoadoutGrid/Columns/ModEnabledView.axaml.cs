using System.Reactive.Disposables;
using Avalonia.Interactivity;
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

            void HandleToggleSwitchCheckedChanged(object? sender, RoutedEventArgs e)
            {
                ViewModel!.ToggleEnabledCommand.Execute(null);
            }

            EnabledToggleSwitch.IsCheckedChanged +=
                HandleToggleSwitchCheckedChanged;
            d.Add(Disposable.Create(() =>
            {
                EnabledToggleSwitch.IsCheckedChanged -=
                    HandleToggleSwitchCheckedChanged;
            }));
        });
    }
}

