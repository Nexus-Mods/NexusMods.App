using System.Reactive.Disposables;
using NexusMods.Abstractions.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public class SettingToggleViewModel : AViewModel<ISettingToggleViewModel>, ISettingToggleViewModel
{
    public BooleanContainer ValueContainer { get; }

    [Reactive] public bool HasChanged { get; private set;  }

    public SettingToggleViewModel(BooleanContainer booleanContainer)
    {
        ValueContainer = booleanContainer;

        this.WhenActivated(disposables =>
        {
            ValueContainer.WhenAnyValue(x => x.HasChanged)
                .BindToVM(this, vm => vm.HasChanged)
                .DisposeWith(disposables);
        });
    }
}
