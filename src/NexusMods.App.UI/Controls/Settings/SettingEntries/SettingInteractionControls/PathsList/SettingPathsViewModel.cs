using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries.PathsList;

public class SettingPathsViewModel : AViewModel<ISettingPathsViewModel>, ISettingPathsViewModel
{
    public IValueContainer ValueContainer => ConfigurablePathsContainer;
    public ConfigurablePathsContainer ConfigurablePathsContainer { get; }
    [Reactive] public bool HasChanged { get; private set; }

    public SettingPathsViewModel(ConfigurablePathsContainer pathsContainer)
    {
        ConfigurablePathsContainer = pathsContainer;

        this.WhenActivated(disposables =>
            {
                ValueContainer.WhenAnyValue(x => x.HasChanged)
                    .BindToVM(this, vm => vm.HasChanged)
                    .DisposeWith(disposables);
            }
        );
    }
}
