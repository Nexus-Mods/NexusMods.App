using System.Reactive;
using System.Reactive.Concurrency;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.ModInfo.Loading;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Diff.ApplyDiff;

public class ApplyDiffViewModel : APageViewModel<IApplyDiffViewModel>, IApplyDiffViewModel
{
    private IServiceProvider _serviceProvider;
    private DiffTreeViewModel? _fileTreeViewModel;
    private LoadoutId _loadoutId;
    private DummyLoadingViewModel _dummyLoadingViewModel;

    [Reactive] public IViewModelInterface BodyViewModel { get; set; }

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }


    public ApplyDiffViewModel(IWindowManager windowManager, IServiceProvider serviceProvider) : base(windowManager)
    {
        TabTitle = Language.ApplyDiffViewModel_PageTitle;
        TabIcon = IconValues.ListFilled;

        _dummyLoadingViewModel = new DummyLoadingViewModel();
        BodyViewModel = _dummyLoadingViewModel;
        _serviceProvider = serviceProvider;

        RefreshCommand = ReactiveCommand.Create( () =>
        {
            if (_fileTreeViewModel is null)
            {
                return;
            }

            BodyViewModel = _dummyLoadingViewModel;
            Refresh(_fileTreeViewModel);
        });
    }


    public void Initialize(LoadoutId loadoutId)
    {
        _loadoutId = loadoutId;
        _fileTreeViewModel = new DiffTreeViewModel(_loadoutId,
            _serviceProvider.GetRequiredService<IApplyService>(),
            _serviceProvider.GetRequiredService<ILoadoutRegistry>()
        );

        Refresh(_fileTreeViewModel);
    }
    
    private void Refresh(DiffTreeViewModel diffTreeViewModel)
    {
        Task.Run(async () =>
            {
                await diffTreeViewModel.Refresh();

                RxApp.MainThreadScheduler.Schedule(() => { BodyViewModel = diffTreeViewModel; });
            }
        );
    }
}
