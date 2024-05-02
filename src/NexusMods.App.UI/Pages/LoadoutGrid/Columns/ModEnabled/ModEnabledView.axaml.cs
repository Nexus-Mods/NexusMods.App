using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.Loadouts.Mods;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModEnabled;

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

            this.BindCommand(ViewModel, vm => vm.DeleteModCommand, view => view.DeleteButton)
                .DisposeWith(d);

            var isCheckedObservable = this.WhenAnyValue(view => view.EnabledToggleSwitch.IsChecked)
                .Select(isChecked => isChecked ?? false);
            
            this.BindCommand(ViewModel, 
                    vm => vm.ToggleEnabledCommand, 
                    view => view.EnabledToggleSwitch,
                    isCheckedObservable)
                .DisposeWith(d);
            
        });
    }

    private void UpdateVisibilities(ModStatus status)
    {
        EnabledToggleSwitch.IsVisible = status == ModStatus.Installed;
        InstallingProgressRing.IsVisible = status == ModStatus.Installing;
        DeleteButton.IsVisible = status == ModStatus.Failed;
    }
}

