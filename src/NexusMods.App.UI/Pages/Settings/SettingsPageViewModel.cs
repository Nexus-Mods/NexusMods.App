using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Controls.Settings.SettingEntries;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Settings;

[UsedImplicitly]
public class SettingsPageViewModel : APageViewModel<ISettingsPageViewModel>, ISettingsPageViewModel
{
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public ReadOnlyObservableCollection<ISettingEntryViewModel> SettingEntries { get; }

    [Reactive]
    public bool HasAnyValueChanged { get; private set; }

    public SettingsPageViewModel(ISettingsManager settingsManager, IWindowManager windowManager) : base(windowManager)
    {
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
        }, this.WhenAnyValue(vm => vm.HasAnyValueChanged));

        CancelCommand = ReactiveCommand.Create(() =>
        {
            // TODO: ask to discard current values
            foreach (var viewModel in SettingEntries)
            {
                viewModel.InteractionControlViewModel.ValueContainer.ResetToPrevious();
            }
        }, this.WhenAnyValue(vm => vm.HasAnyValueChanged));

        CloseCommand = ReactiveCommand.Create(() => {});

        this.WhenActivated(disposables =>
        {
            var workspaceController = GetWorkspaceController();
            workspaceController.SetTabTitle(Language.SettingsView_Title, WorkspaceId, PanelId, TabId);
            workspaceController.SetIcon(IconValues.Settings, WorkspaceId, PanelId, TabId);

            SettingEntries
                .Select(vm => vm.WhenAnyValue(x => x.InteractionControlViewModel.ValueContainer.HasChanged))
                .Merge()
                .SubscribeWithErrorLogging(_ =>
                {
                    HasAnyValueChanged = SettingEntries.Any(x => x.InteractionControlViewModel.ValueContainer.HasChanged);
                })
                .DisposeWith(disposables);
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
