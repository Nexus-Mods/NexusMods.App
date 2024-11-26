using System.Reactive;
using System.Reactive.Disposables;
using NexusMods.Abstractions.Games;
using NexusMods.App.UI.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrderItemModel : TreeDataGridItemModel<ILoadOrderItemModel, Guid>, ILoadOrderItemModel
{
    private CompositeDisposable _disposables = new();
    public ISortableItem InnerItem { get; }
    
    public ReactiveCommand<Unit, Unit> MoveUp { get; }
    public ReactiveCommand<Unit, Unit> MoveDown { get; }
    public int SortIndex { get; }
    public string DisplayName { get; }
    
    [Reactive] public string ModName { get; private set; }
    [Reactive] public bool IsActive { get; private set; }

    public LoadOrderItemModel(ISortableItem sortableItem)
    {
        InnerItem = sortableItem;
        SortIndex = sortableItem.SortIndex;
        DisplayName = sortableItem.DisplayName;
        
        IsActive = sortableItem.IsActive;
        ModName = sortableItem.ModName;
        
        MoveUp = ReactiveCommand.CreateFromTask(async () =>
            {
                await sortableItem.SortableItemProvider.SetRelativePosition(InnerItem, delta: 1);
                return Unit.Default;
            }
        );
        
        MoveDown = ReactiveCommand.CreateFromTask(async () =>
            {
                await sortableItem.SortableItemProvider.SetRelativePosition(InnerItem, delta: -1);
                return Unit.Default;
            }
        );

        this.WhenAnyValue(vm => vm.InnerItem.IsActive)
            .Subscribe(value => IsActive = value)
            .DisposeWith(_disposables);

        this.WhenAnyValue(vm => vm.InnerItem.ModName)
            .Subscribe(value => ModName = value)
            .DisposeWith(_disposables);
    }
    

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _disposables.Dispose();
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
