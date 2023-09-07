using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

internal static class GuidedInstallerStepViewModelHelpers
{
    public static void SetupCrossGroupOptionHighlighting<T>(this T viewModel, CompositeDisposable disposables)
        where T : IGuidedInstallerStepViewModel
    {
        viewModel.WhenAnyValue(x => x.Groups)
            .Select(groupVMs => groupVMs
                .Select(groupVM => groupVM
                    .WhenAnyValue(x => x.HighlightedOption)
                )
                .CombineLatest()
            )
            .SubscribeWithErrorLogging(logger: default, observable =>
            {
                observable
                    .SubscribeWithErrorLogging(logger: default, list =>
                    {
                        var previous = viewModel.HighlightedOptionViewModel;
                        if (previous is null)
                        {
                            viewModel.HighlightedOptionViewModel = list.FirstOrDefault(x => x is not null);
                            return;
                        }

                        var highlightedOptionVMs = list
                            .Where(x => x is not null)
                            .Select(x => x!)
                            .ToArray();

                        var newVM = highlightedOptionVMs.First(x => x.Option.Id != previous.Option.Id);
                        viewModel.HighlightedOptionViewModel = newVM;

                        foreach (var groupVM in viewModel.Groups)
                        {
                            if (groupVM.HighlightedOption != previous) continue;
                            groupVM.HighlightedOption = null;
                        }
                    })
                    .DisposeWith(disposables);
            })
            .DisposeWith(disposables);
    }
}
