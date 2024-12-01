using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

[UsedImplicitly]
public partial class SettingToggleControl : ReactiveUserControl<ISettingToggleViewModel>
{
    public SettingToggleControl()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.BooleanContainer.CurrentValue, view => view.ToggleSwitch.IsChecked)
                .DisposeWith(disposables);
        });
    }
}

