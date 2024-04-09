using System.Reactive;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.ModInfo.Loading;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Diff.ApplyDiff;

public class ApplyDiffViewModel : APageViewModel<IApplyDiffViewModel>, IApplyDiffViewModel
{
    private IServiceProvider _serviceProvider;
    private DiffTreeViewModel? _fileTreeViewModel;
    private LoadoutId _loadoutId;
    private DummyLoadingViewModel _dummyLoadingViewModel;
    
    public IFileTreeViewModel? FileTreeViewModel => _fileTreeViewModel;
    [Reactive] public IViewModelInterface BodyViewModel { get; set; }
    
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    
    public ApplyDiffViewModel(IWindowManager windowManager, IServiceProvider serviceProvider) : base(windowManager)
    {
        _dummyLoadingViewModel = new DummyLoadingViewModel();
        BodyViewModel = _dummyLoadingViewModel;
        _serviceProvider = serviceProvider;
        
        RefreshCommand = ReactiveCommand.Create(() =>
        {
            if (_fileTreeViewModel is null)
            {
                return;
            }
            BodyViewModel = _dummyLoadingViewModel;
            _fileTreeViewModel?.Refresh();
            BodyViewModel = _fileTreeViewModel!;
        });
    }

    
    public void Initialize(LoadoutId loadoutId)
    {
        _loadoutId = loadoutId;
        _fileTreeViewModel = new DiffTreeViewModel(_loadoutId, 
            _serviceProvider.GetRequiredService<IApplyService>(),
            _serviceProvider.GetRequiredService<ILoadoutRegistry>());
        _fileTreeViewModel.Refresh();
        BodyViewModel = _fileTreeViewModel;
    }

}
