using System.Reactive.Disposables;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Controls;
using NexusMods.DataModel.Loadouts;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

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
                .BindToUi(this, view => view.EnabledToggleSwitch.IsChecked)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Status)
                .OnUI()
                .Subscribe(UpdateVisibilities)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.DeleteModCommand, view => view.DeleteButton)
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

    private void UpdateVisibilities(ModStatus status)
    {
        EnabledToggleSwitch.IsVisible = status == ModStatus.Installed;
        InstallingProgressRing.IsVisible = status == ModStatus.Installing;
        DeleteButton.IsVisible = status == ModStatus.Failed;
    }
}

