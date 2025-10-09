using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.GuidedInstallers.ValueObjects;
using NexusMods.App.UI;
using NexusMods.Games.FOMOD.UI.Resources;
using NexusMods.UI.Sdk;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerGroupViewModel : AViewModel<IGuidedInstallerGroupViewModel>, IGuidedInstallerGroupViewModel
{
    public IObservable<bool> HasValidSelectionObservable { get; }

    public OptionGroup Group { get; }

    private readonly SourceCache<IGuidedInstallerOptionViewModel, OptionId> _optionsCache = new(x => x.Option.Id);
    private readonly ReadOnlyObservableCollection<IGuidedInstallerOptionViewModel> _options;
    public ReadOnlyObservableCollection<IGuidedInstallerOptionViewModel> Options => _options;

    [Reactive] public IGuidedInstallerOptionViewModel? HighlightedOption { get; set; }

    public GuidedInstallerGroupViewModel(OptionGroup group)
    {
        Group = group;

        _optionsCache
            .Connect()
            .Bind(out _options)
            .Subscribe();

        _optionsCache.Edit(updater =>
        {
            var options = group.Options.Select(option => new GuidedInstallerOptionViewModel(option, group));
            if (group.Type == OptionGroupType.AtMostOne)
            {
                options = options.Prepend(new GuidedInstallerOptionViewModel(new Option
                {
                    Id = OptionId.DefaultValue,
                    Name = Language.GuidedInstallerGroupViewModel_GuidedInstallerGroupViewModel_None,
                    Type = OptionType.Available,
                    Description = Language.GuidedInstallerGroupViewModel_GuidedInstallerGroupViewModel_Select_nothing,
                    HoverText = Language
                        .GuidedInstallerGroupViewModel_GuidedInstallerGroupViewModel_Use_this_option_to_select_nothing
                }, group));
            }

            updater.AddOrUpdate(options);
        });

        if (group.Type.UsesRadioButtons())
        {
            // NOTE(erri120): If none of the options are pre-selected, we select the first valid one.
            var hasPreSelectedOptions = Options.Any(x => x.IsChecked);
            if (!hasPreSelectedOptions)
            {
                var firstOption = Options.FirstOrDefault(x => x.IsEnabled);
                if (firstOption is not null)
                {
                    firstOption.IsChecked = true;
                }
            }

            // NOTE(erri120): If one option is checked and disabled, we must disable the entire group.
            var hasDisabledCheckedOption = Options.Any(x => x is { IsChecked: true, IsEnabled: false });
            if (hasDisabledCheckedOption)
            {
                foreach (var optionVM in Options)
                {
                    optionVM.IsEnabled = false;
                }
            }
        }

        // NOTE(erri120): This is the only group type that requires validations as it uses checkboxes in the UI.
        if (Group.Type is OptionGroupType.AtLeastOne)
        {
            HasValidSelectionObservable = _optionsCache
                .Connect()
                .WhenValueChanged(optionVM => optionVM.IsChecked)
                .Select(_ =>
                {
                    var selectedOptions = Options
                        .Where(option => option.IsChecked)
                        .Select(option => option.Option.Id)
                        .Where(optionId => optionId != OptionId.DefaultValue)
                        .Select(optionId => new SelectedOption(Group.Id, optionId))
                        .ToArray();

                    return GuidedInstallerValidation.IsValidGroupSelection(Group, selectedOptions);
                });
        }
        else
        {
            HasValidSelectionObservable = Observable.Return(true);
        }

        this.WhenActivated(disposable =>
        {
            // propagate validation status to options
            this.WhenAnyObservable(vm => vm.HasValidSelectionObservable)
                .Subscribe(isValid =>
                {
                    foreach (var optionVM in Options)
                    {
                        optionVM.IsValid = isValid;
                    }
                })
                .DisposeWith(disposable);

            // Highlight option when it is checked
            _optionsCache
                .Connect()
                .WhenPropertyChanged(optionVM => optionVM.IsChecked, false)
                .SubscribeWithErrorLogging(option =>
                {
                    HighlightedOption = option.Sender;
                })
                .DisposeWith(disposable);
        });
    }
}
