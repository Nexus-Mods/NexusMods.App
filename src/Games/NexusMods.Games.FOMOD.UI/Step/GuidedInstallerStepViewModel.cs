using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.GuidedInstallers.ValueObjects;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI;
using NexusMods.App.UI.Extensions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

[UsedImplicitly]
public class GuidedInstallerStepViewModel : AViewModel<IGuidedInstallerStepViewModel>, IGuidedInstallerStepViewModel
{
    [Reactive] public string ModName { get; set; } = string.Empty;

    [Reactive] public bool ShowInstallationCompleteScreen { get; private set; }
    [Reactive] public GuidedInstallationStep? InstallationStep { get; set; }

    private readonly SourceCache<IGuidedInstallerGroupViewModel, GroupId> _groupsSource = new(x => x.Group.Id);
    private readonly ReadOnlyObservableCollection<IGuidedInstallerGroupViewModel> _groups;
    public ReadOnlyObservableCollection<IGuidedInstallerGroupViewModel> Groups => _groups;

    [Reactive] public IGuidedInstallerOptionViewModel? HighlightedOptionViewModel { get; set; }

    private IDisposable? _imageDisposable;
    private readonly ObservableAsPropertyHelper<IImage?> _highlightedOptionImage;
    public IImage? HighlightedOptionImage => _highlightedOptionImage.Value;

    [Reactive] public Percent Progress { get; set; }
    public IFooterStepperViewModel FooterStepperViewModel { get; } = new FooterStepperViewModel();

    [Reactive] public TaskCompletionSource<UserChoice>? TaskCompletionSource { get; set; }

    [Reactive] public bool HasValidSelections { get; set; }

    private Percent _previousProgress = Percent.Zero;

    public GuidedInstallerStepViewModel(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<GuidedInstallerStepViewModel>>();

        var remoteImagePipeline = ImagePipelines.GetGuidedInstallerRemoteImagePipeline(serviceProvider);
        var fileImagePipeline = ImagePipelines.GetGuidedInstallerFileImagePipeline(serviceProvider);

        _groupsSource
            .Connect()
            .Bind(out _groups)
            .Subscribe();

        // image loading
        _highlightedOptionImage = this
            .WhenAnyValue(vm => vm.HighlightedOptionViewModel)
            .WhereNotNull()
            .Select(optionVM => optionVM.Option.Image)
            .WhereNotNull()
            .OffUi()
            .SelectMany(async optionImage =>
            {
                try
                {
                    return await optionImage.Match(
                        f0: uri => remoteImagePipeline.LoadResourceAsync(uri, CancellationToken.None),
                        f1: imageHash => fileImagePipeline.LoadResourceAsync(imageHash.FileHash, CancellationToken.None)
                    );
                }
                catch (Exception e)
                {
                    logger.LogWarning(e, "Failed to load image");
                    return null;
                }
            })
            .Select(static resource => resource?.Data)
            .Do(lifetime =>
            {
                _imageDisposable?.Dispose();
                _imageDisposable = lifetime;
            })
            .Select(static lifetime => lifetime?.Value)
            .WhereNotNull()
            .OnUI()
            .ToProperty(this, vm => vm.HighlightedOptionImage);

        var goToNextCommand = ReactiveCommand.Create(() =>
        {
            _imageDisposable?.Dispose();
            _imageDisposable = null;

            // NOTE(erri120): On the last step, we don't set the result but instead show a "installation complete"-screen.
            if (InstallationStep!.HasNextStep || ShowInstallationCompleteScreen)
            {
                var selectedOptions = GatherSelectedOptions();
                TaskCompletionSource?.TrySetResult(new UserChoice(new UserChoice.GoToNextStep(selectedOptions)));
            }
            else
            {
                _previousProgress = Progress;
                Progress = Percent.One;
                ShowInstallationCompleteScreen = true;
            }
        });

        var goToPrevCommand = ReactiveCommand.Create(() =>
        {
            _imageDisposable?.Dispose();
            _imageDisposable = null;

            if (ShowInstallationCompleteScreen)
            {
                ShowInstallationCompleteScreen = false;
                Progress = _previousProgress;
            }
            else
            {
                TaskCompletionSource?.TrySetResult(new UserChoice(new UserChoice.GoToPreviousStep()));
            }
        });

        this.WhenActivated(disposables =>
        {
            // create new groups when the step changes
            this.WhenAnyValue(vm => vm.InstallationStep)
                .Subscribe(step =>
                {
                    _groupsSource.Edit(updater =>
                    {
                        updater.Clear();
                        if (step is null) return;

                        var groups = step.Groups.Select(group => new GuidedInstallerGroupViewModel(group));
                        updater.AddOrUpdate(groups);
                    });

                    // highlight the first option in the first group as a default
                    if (_groupsSource.Count != 0)
                    {
                        var group = _groupsSource.Items.First();
                        group.HighlightedOption = group.Options.First();
                    }
                })
                .DisposeWith(disposables);

            // group highlighting
            _groupsSource
                .Connect()
                .WhenPropertyChanged(groupVM => groupVM.HighlightedOption, false)
                .Where(propertyValue => propertyValue.Value is not null)
                .Do(propertyValue =>
                {
                    HighlightedOptionViewModel = propertyValue.Value;

                    foreach (var groupVM in Groups)
                    {
                        if (ReferenceEquals(propertyValue.Sender, groupVM)) continue;
                        groupVM.HighlightedOption = null;
                    }
                })
                .Select(propertyValue => propertyValue.Value)
                .BindToVM(this, vm => vm.HighlightedOptionViewModel)
                .DisposeWith(disposables);

            // group validation results
            _groupsSource
                .Connect()
                .TrueForAll(groupVM => groupVM.HasValidSelectionObservable, b => b)
                .BindToVM(this, vm => vm.HasValidSelections)
                .DisposeWith(disposables);

            // CanGoNext
            this.WhenAnyValue(
                    vm => vm.TaskCompletionSource,
                    vm => vm.InstallationStep,
                    vm => vm.HasValidSelections,
                    (tcs, step, hasValidSelections) => tcs is not null && step is not null && hasValidSelections)
                .BindToVM(this, vm => vm.FooterStepperViewModel.CanGoNext)
                .DisposeWith(disposables);

            // CanGoPrev
            this.WhenAnyValue(
                    vm => vm.TaskCompletionSource,
                    vm => vm.InstallationStep,
                    vm => vm.ShowInstallationCompleteScreen,
                    (tcs, step, showInstallationCompleteScreen) => showInstallationCompleteScreen ||
                                                                   (tcs is not null && step is not null &&
                                                                    step.HasPreviousStep))
                .BindToVM(this, vm => vm.FooterStepperViewModel.CanGoPrev)
                .DisposeWith(disposables);

            // GoToNext
            this.WhenAnyObservable(vm => vm.FooterStepperViewModel.GoToNextCommand)
                .InvokeReactiveCommand(goToNextCommand)
                .DisposeWith(disposables);

            // GoToPrev
            this.WhenAnyObservable(vm => vm.FooterStepperViewModel.GoToPrevCommand)
                .InvokeReactiveCommand(goToPrevCommand)
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.Progress)
                .BindToVM(this, vm => vm.FooterStepperViewModel.Progress)
                .DisposeWith(disposables);

            Disposable.Create(() =>
            {
                _imageDisposable?.Dispose();
                _highlightedOptionImage.Dispose();
            }).DisposeWith(disposables);
        });
    }

    private SelectedOption[] GatherSelectedOptions()
    {
        return Groups
            .SelectMany(groupVM => groupVM.Options
                .Where(optionVM => optionVM.IsChecked && optionVM.Option.Id != OptionId.DefaultValue)
                .Select(optionVM => new SelectedOption(groupVM.Group.Id, optionVM.Option.Id))
            )
            .ToArray();
    }
}
