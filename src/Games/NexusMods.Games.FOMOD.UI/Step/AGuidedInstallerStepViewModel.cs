using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public abstract class AGuidedInstallerStepViewModel : AViewModel<IGuidedInstallerStepViewModel>, IGuidedInstallerStepViewModel
{
    [Reactive]
    public virtual string? ModName { get; set; }

    [Reactive]
    public bool ShowInstallationCompleteScreen { get; protected set; }
    [Reactive]
    public GuidedInstallationStep? InstallationStep { get; set; }

    [Reactive]
    public IGuidedInstallerGroupViewModel[] Groups { get; protected set; } = Array.Empty<IGuidedInstallerGroupViewModel>();
    [Reactive]
    public IGuidedInstallerOptionViewModel? HighlightedOptionViewModel { get; set; }

    private readonly Subject<IImage> _highlightedOptionImageSubject = new();
    public IObservable<IImage> HighlightedOptionImageObservable => _highlightedOptionImageSubject;

    [Reactive]
    public Percent Progress { get; set; }

    public abstract IFooterStepperViewModel FooterStepperViewModel { get; }

    [Reactive]
    public TaskCompletionSource<UserChoice>? TaskCompletionSource { get; set; }

    [Reactive]
    public bool HasValidSelections { get; set; }

    private bool _hadPreviousGroups;

    // NOTE (erri120): We use this custom CompositeDisposable for dealing with changes
    // to the Groups property. Multiple observers of this property create new observables
    // that subscribe to nested changes that have to be disposed of when the Groups property
    // changes. This CompositeDisposable allows for such behavior.
    private CompositeDisposable _groupDisposables = new();

    private readonly IImageCache _imageCache;
    protected AGuidedInstallerStepViewModel(IImageCache imageCache)
    {
        _imageCache = imageCache;

        this.WhenActivated(disposables =>
        {
            // group cleanup
            this
                .WhenAnyValue(x => x.Groups)
                .SubscribeWithErrorLogging(_ =>
                {
                    // NOTE(erri120): This looks a bit weird, but the issue
                    // is that we have multiple WhenAnyValue calls that react
                    // to changes to the Groups property and it's possible
                    // that this observer gets called last, when all the other
                    // observers have already used the CompositeDisposable.
                    // As such, we have this field to help with ordering.
                    if (!_hadPreviousGroups)
                    {
                        _hadPreviousGroups = true;
                        return;
                    }

                    _groupDisposables.Dispose();
                    disposables.Remove(_groupDisposables);

                    _groupDisposables = new CompositeDisposable();
                    disposables.Add(_groupDisposables);
                })
                .DisposeWith(disposables);

            // image loading
            this
                .WhenAnyValue(x => x.HighlightedOptionViewModel)
                .WhereNotNull()
                .Select(optionVM => optionVM.Option.Image)
                .WhereNotNull()
                .OffUi()
                .Select(optionImage => Observable.FromAsync(() => _imageCache.GetImage(optionImage, cancellationToken: default)))
                .Concat()
                .WhereNotNull()
                .OnUI()
                .SubscribeWithErrorLogging(_highlightedOptionImageSubject.OnNext)
                .DisposeWith(disposables);

            // cross-group option highlighting
            this
                .WhenAnyValue(x => x.Groups)
                .Select(groupVMs => groupVMs
                    .Select(groupVM => groupVM.WhenAnyValue(x => x.HighlightedOption))
                    .CombineLatest()
                )
                .SubscribeWithErrorLogging(observable =>
                {
                    observable
                        .SubscribeWithErrorLogging(list =>
                        {
                            var previous = HighlightedOptionViewModel;
                            if (previous is null)
                            {
                                // Highlight the first option of the first group as a default.
                                var group = Groups.First();
                                group.HighlightedOption = group.Options.First();
                                HighlightedOptionViewModel = group.HighlightedOption;
                                return;
                            }

                            var optionVMs = list
                                .Where(vm => vm is not null)
                                .Select(vm => vm!)
                                .ToArray();

                            // NOTE(erri120): This is a bit weird, but the issue is that we're reacting to changes
                            // of the HighlightedOption property while also changing this property inside this observer.
                            // As such, we need to make sure that we only react to the initial change that the user did,
                            // and not to the change we made ourselves.
                            var newVM = optionVMs.FirstOrDefault(vm => vm.Option.Id != previous.Option.Id);
                            if (newVM is null) return;

                            HighlightedOptionViewModel = newVM;

                            foreach (var groupVM in Groups)
                            {
                                if (groupVM.HighlightedOption != previous) continue;
                                groupVM.HighlightedOption = null;
                                break;
                            }
                        })
                        .DisposeWith(_groupDisposables);
                })
                .DisposeWith(disposables);

            // gathering group validation results
            this
                .WhenAnyValue(x => x.Groups)
                .Select(groupVMs => groupVMs
                    .Select(groupVM => groupVM.WhenAnyValue(x => x.HasValidSelection))
                    .CombineLatest()
                    .Select(list => list.All(isValid => isValid))
                )
                .SubscribeWithErrorLogging(observable =>
                {
                    observable
                        .SubscribeWithErrorLogging(allValid => { HasValidSelections = allValid; })
                        .DisposeWith(_groupDisposables);
                })
                .DisposeWith(disposables);
        });
    }

    protected SelectedOption[] GatherSelectedOptions()
    {
        return Groups
            .SelectMany(groupVM => groupVM.Options
                .Where(optionVM => optionVM.IsChecked && optionVM.Option.Id != OptionId.None)
                .Select(optionVM => new SelectedOption(groupVM.Group.Id, optionVM.Option.Id))
            )
            .ToArray();
    }
}
