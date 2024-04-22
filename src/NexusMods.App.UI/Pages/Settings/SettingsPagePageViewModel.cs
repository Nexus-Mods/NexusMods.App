using System.Collections.ObjectModel;
using System.Reactive;
using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Controls.Settings.SettingEntries;
using NexusMods.App.UI.Controls.Settings.SettingEntries.SettingInteractionControls;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Settings;

[UsedImplicitly]
public class SettingsPageViewModel : APageViewModel<ISettingsPageViewModel>, ISettingsPageViewModel
{
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public ReadOnlyObservableCollection<ISettingEntryViewModel> SettingEntries { get; }

    public SettingsPageViewModel(ISettingsManager settingsManager, IWindowManager windowManager) : base(windowManager)
    {
        SaveCommand = ReactiveCommand.Create(() => { });
        CancelCommand = ReactiveCommand.Create(() => { });
        CloseCommand = ReactiveCommand.Create(() => { });

        var descriptors = settingsManager.GetAllUIProperties();
        var entryViewModels = descriptors.Select(CreateEntryViewModel).ToArray();

        // ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
        SettingEntries = new(new(entryViewModels));
        // ReSharper restore ArrangeObjectCreationWhenTypeNotEvident
    }

    private ISettingEntryViewModel CreateEntryViewModel(ISettingsPropertyUIDescriptor descriptor)
    {
        var valueContainer = descriptor.SettingsPropertyValueContainer;
        var interactionControl = valueContainer.Match<IViewModelInterface>(
            f0: booleanContainer => new SettingToggleViewModel(booleanContainer),
            // TODO:
            f1: singleValueMultipleChoiceContainer => new SettingToggleViewModel(new BooleanContainer(value: true, defaultValue: false))
        );

        var res = new SettingEntryViewModel(descriptor, interactionControl);
        return res;
    }
}
