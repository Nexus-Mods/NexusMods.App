using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;
using NexusMods.Games.FOMOD.UI.Resources;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerGroupViewModel : AViewModel<IGuidedInstallerGroupViewModel>, IGuidedInstallerGroupViewModel
{
    [Reactive]
    public bool HasValidSelection { get; set; } = true;

    public OptionGroup Group { get; }

    public IGuidedInstallerOptionViewModel[] Options { get; }

    [Reactive]
    public IGuidedInstallerOptionViewModel? HighlightedOption { get; set; }

    public GuidedInstallerGroupViewModel(OptionGroup group) : this(group, option => new GuidedInstallerOptionViewModel(option, group)) { }

    protected GuidedInstallerGroupViewModel(OptionGroup group, Func<Option, IGuidedInstallerOptionViewModel> factory)
    {
        Group = group;

        var options = group.Options.Select(factory);
        if (group.Type == OptionGroupType.AtMostOne)
        {
            Options = options
                .Prepend(factory(new Option
                {
                    Id = OptionId.None,
                    Name = Language.GuidedInstallerGroupViewModel_GuidedInstallerGroupViewModel_None,
                    Type = OptionType.Available,
                    Description = Language.GuidedInstallerGroupViewModel_GuidedInstallerGroupViewModel_Select_nothing,
                    HoverText = Language.GuidedInstallerGroupViewModel_GuidedInstallerGroupViewModel_Use_this_option_to_select_nothing
                }))
                .ToArray();
        }
        else
        {
            Options = options.ToArray();
        }

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

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.HasValidSelection)
                .SubscribeWithErrorLogging(isValid =>
                {
                    foreach (var optionVM in Options)
                    {
                        optionVM.IsValid = isValid;
                    }
                })
                .DisposeWith(disposables);

            // NOTE(erri120): This is the only group type that requires validation as
            // it uses checkboxes in the UI.
            if (Group.Type is OptionGroupType.AtLeastOne)
            {
                Options
                    .Select(optionVM => optionVM
                        .WhenAnyValue(x => x.IsChecked)
                        .Select(isChecked => (optionVM.Option.Id, isChecked)))
                    .CombineLatest()
                    .SubscribeWithErrorLogging(values =>
                    {
                        var selectedOptions = values
                            .Where(tuple => tuple.isChecked)
                            .Select(tuple => tuple.Id)
                            .Where(optionId => optionId != OptionId.None)
                            .Select(optionId => new SelectedOption(Group.Id, optionId))
                            .ToArray();

                        HasValidSelection = GuidedInstallerValidation.IsValidGroupSelection(Group, selectedOptions);
                    })
                    .DisposeWith(disposables);
            }
        });
    }
}
