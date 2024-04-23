using System.Collections.ObjectModel;
using System.Reactive;
using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Controls.Settings.SettingEntries;
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
        CancelCommand = ReactiveCommand.Create(() => { });
        CloseCommand = ReactiveCommand.Create(() => { });

        var descriptors = settingsManager.GetAllUIProperties();
        var entryViewModels = descriptors.Select(CreateEntryViewModel).ToArray();

        // ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
        SettingEntries = new(new(entryViewModels));
        // ReSharper restore ArrangeObjectCreationWhenTypeNotEvident

        SaveCommand = ReactiveCommand.Create(() =>
        {
            var changedEntries = SettingEntries
                .Where(vm => vm.InteractionControlViewModel.ValueContainer.HasChanged)
                .ToArray();

            if (changedEntries.Length == 0) return;
            foreach (var viewModel in changedEntries)
            {
                viewModel.InteractionControlViewModel.ValueContainer.Update(settingsManager);
            }
        });
    }

    private ISettingEntryViewModel CreateEntryViewModel(ISettingsPropertyUIDescriptor descriptor)
    {
        var valueContainer = descriptor.SettingsPropertyValueContainer;
        var interactionControl = valueContainer.Match<ISettingInteractionControl>(
            f0: booleanContainer => new SettingToggleViewModel(booleanContainer),
            f1: singleValueMultipleChoiceContainer => new SettingComboBoxViewModel(singleValueMultipleChoiceContainer)
        );

        var res = new SettingEntryViewModel(descriptor, interactionControl);
        return res;
    }
}
