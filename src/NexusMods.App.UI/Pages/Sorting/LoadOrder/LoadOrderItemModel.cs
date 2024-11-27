using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.Games;
using NexusMods.App.UI.Controls;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;
using Observable = System.Reactive.Linq.Observable;
using Unit = System.Reactive.Unit;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrderItemModel : TreeDataGridItemModel<ILoadOrderItemModel, Guid>, ILoadOrderItemModel
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IObservable<ListSortDirection> _sortDirectionObservable;
    private readonly IObservable<int> _lastIndexObservable;
    public ISortableItem InnerItem { get; }

    public R3.ReactiveCommand<Unit, Unit> MoveUp { get; }
    public R3.ReactiveCommand<Unit, Unit> MoveDown { get; } = new(_ => Unit.Default);
    public int SortIndex { get; }
    public string DisplayName { get; }

    [Reactive] public string ModName { get; private set; }
    [Reactive] public bool IsActive { get; private set; }

    public LoadOrderItemModel(
        ISortableItem sortableItem,
        IObservable<ListSortDirection> sortDirectionObservable,
        IObservable<int> lastIndexObservable,
        Subject<MoveUpDownCommandPayload> commandSubject)
    {
        InnerItem = sortableItem;
        SortIndex = sortableItem.SortIndex;
        DisplayName = sortableItem.DisplayName;

        IsActive = sortableItem.IsActive;
        ModName = sortableItem.ModName;

        _sortDirectionObservable = sortDirectionObservable;
        _lastIndexObservable = lastIndexObservable;

        this.WhenAnyValue(vm => vm.InnerItem.IsActive)
            .Subscribe(value => IsActive = value)
            .DisposeWith(_disposables);

        this.WhenAnyValue(vm => vm.InnerItem.ModName)
            .Subscribe(value => ModName = value)
            .DisposeWith(_disposables);

        var sortIndexObservable = this.WhenAnyValue(vm => vm.SortIndex);
        var canExecuteUp =  Observable.CombineLatest(
            sortIndexObservable, _sortDirectionObservable, _lastIndexObservable,
                (sortIndex, sortDirection, lastIndex) =>
                    sortDirection == ListSortDirection.Ascending ? sortIndex > 0 : sortIndex < lastIndex
            )
            .ToObservable();

        var canExecuteDown = Observable.CombineLatest(
                sortIndexObservable, _sortDirectionObservable, _lastIndexObservable,
                (sortIndex, sortDirection, lastIndex) =>
                    sortDirection == ListSortDirection.Descending ? sortIndex > 0 : sortIndex < lastIndex
            )
            .ToObservable();

        MoveUp = canExecuteUp.ToReactiveCommand<Unit, Unit>(_ => Unit.Default);
        MoveDown = canExecuteDown.ToReactiveCommand<Unit, Unit>(_ => Unit.Default);
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
