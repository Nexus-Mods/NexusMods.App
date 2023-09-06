using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerGroupViewModel : AViewModel<IGuidedInstallerGroupViewModel>, IGuidedInstallerGroupViewModel
{
    public OptionGroup Group { get; }

    public IGuidedInstallerOptionViewModel[] Options { get; }

    [Reactive]
    public IGuidedInstallerOptionViewModel? HighlightedOption { get; set; }

    public GuidedInstallerGroupViewModel(OptionGroup group) : this(group, option => new GuidedInstallerOptionViewModel(option)) { }

    protected GuidedInstallerGroupViewModel(OptionGroup group, Func<Option, IGuidedInstallerOptionViewModel> factory)
    {
        Group = group;
        Options = group.Options
            .Select(factory)
            .ToArray();

        // this.WhenActivated(disposable =>
        // {
        //     Options
        //         .Select(optionsVM => optionsVM
        //             .WhenAnyValue(x => x.IsHighlighted)
        //             .Select(isHighlighted => isHighlighted ? optionsVM.Option : null))
        //         .CombineLatest()
        //         .SubscribeWithErrorLogging(logger: default, options =>
        //         {
        //             var previousOption = HighlightedOption;
        //             if (previousOption is null)
        //             {
        //                 HighlightedOption = options.FirstOrDefault(x => x is not null);
        //                 return;
        //             }
        //
        //             var highlightedOptions = options
        //                 .Where(x => x is not null)
        //                 .Select(x => x!)
        //                 .ToArray();
        //
        //             switch (highlightedOptions.Length)
        //             {
        //                 case 0:
        //                     // no option is highlighted
        //                     HighlightedOption = null;
        //                     return;
        //                 case 2:
        //                 {
        //                     // two options are highlighted, one of those is the "new" option, the other one
        //                     // is the previous one.
        //                     var newOption = highlightedOptions.First(x => x.Id != previousOption.Id);
        //                     HighlightedOption = newOption;
        //                     break;
        //                 }
        //                 case 1 when highlightedOptions[0].Id == previousOption.Id:
        //                     // only one option is highlighted, this MUST be the currently one
        //
        //                     // NOTE(erri120): this case is possible because we're listening for
        //                     // changes to the IsHighlighted property, which we're changing inside
        //                     // this method.
        //                     return;
        //                 case 1:
        //                     // NOTE(erri120): this case SHOULD be impossible
        //                     throw new UnreachableException();
        //             }
        //
        //             var previousOptionsVM = Options.FirstOrDefault(x => x.Option.Id == previousOption.Id);
        //             if (previousOptionsVM is not null)
        //             {
        //                 previousOptionsVM.IsHighlighted = false;
        //             }
        //         })
        //         .DisposeWith(disposable);
        // });
    }
}
