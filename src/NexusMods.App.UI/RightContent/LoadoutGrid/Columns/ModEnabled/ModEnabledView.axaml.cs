using System.Reactive.Disposables;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModEnabled;

public partial class ModEnabledView : ReactiveUserControl<IModEnabledViewModel>
{
    public ModEnabledView()
    {
        InitializeComponent();

        EnabledToggleSwitch.IsVisible = false;
        InstallingProgressRing.IsVisible = false;
        DeleteButton.IsVisible = false;

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.Enabled)
                .BindToUi<bool, ModEnabledView, bool?>(this, view => view.EnabledToggleSwitch.IsChecked)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Status)
                .OnUI()
                .SubscribeWithErrorLogging(logger: default, UpdateVisibilities)
                .DisposeWith(d);

            this.BindCommand<ModEnabledView, IModEnabledViewModel, ICommand, Button>(ViewModel, vm => vm.DeleteModCommand, view => view.DeleteButton)
                .DisposeWith(d);

            void HandleToggleSwitchCheckedChanged(object? sender, RoutedEventArgs e)
            {
                ViewModel!.ToggleEnabledCommand.Execute(EnabledToggleSwitch.IsChecked);
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

    private void UpdateVisibilities(ModStatus status)
    {
        EnabledToggleSwitch.IsVisible = status == ModStatus.Installed;
        InstallingProgressRing.IsVisible = status == ModStatus.Installing;
        DeleteButton.IsVisible = status == ModStatus.Failed;
    }
}

