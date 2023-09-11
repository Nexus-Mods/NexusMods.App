using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;
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

    public static void SetupHighlightedOption<T>(
        this T viewModel,
        Subject<IImage> highlightedOptionImageSubject,
        CompositeDisposable disposables)
        where T : IGuidedInstallerStepViewModel
    {
        viewModel
            .WhenAnyValue(x => x.HighlightedOptionViewModel)
            .WhereNotNull()
            .Select(optionVM => optionVM.Option)
            .Where(option =>
            {
                viewModel.HighlightedOptionDescription = option.Description;
                return option.ImageUrl is not null;
            })
            .OffUi()
            .Select(option =>
            {
                // TODO: local files (see issue #614)
                var imageUrl = option.ImageUrl!.Value.Value;
                return !Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ? null : uri;
            })
            .WhereNotNull()
            .OffUi()
            .Select(uri => Observable.FromAsync(() => LoadRemoteImage(uri, cancellationToken: default)))
            .Concat()
            .WhereNotNull()
            .OnUI()
            .SubscribeWithErrorLogging(logger: default, highlightedOptionImageSubject.OnNext)
            .DisposeWith(disposables);
    }

    private static async Task<Bitmap?> LoadRemoteImage(Uri uri, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = new HttpClient();
            var stream = await client.GetByteArrayAsync(uri, cancellationToken);
            return new Bitmap(new MemoryStream(stream));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    public static readonly OptionId NoneOptionId = OptionId.From(Guid.Empty);

    public static SelectedOption[] GatherSelectedOptions<T>(this T viewModel)
        where T : IGuidedInstallerStepViewModel
    {
        return viewModel.Groups
            .SelectMany(groupVM => groupVM.Options
                .Where(optionVM => optionVM.IsSelected)
                // NOTE(erri120): The "None" option gets added programatically
                // and allows the user to make no choice at all. As such,
                // we must not forward this option as selected.
                .Where(optionVM => optionVM.Option.Id != NoneOptionId)
                .Select(optionVM => new SelectedOption(groupVM.Group.Id, optionVM.Option.Id))
            )
            .ToArray();
    }
}
