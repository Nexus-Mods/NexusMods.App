using System.ComponentModel;
using System.Reactive.Disposables;
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
    private ListSortDirection _sortDirection;
    public ISortableItem InnerItem { get; }

    public ReactiveUI.ReactiveCommand<Unit, Unit> MoveUp { get; }
    public ReactiveUI.ReactiveCommand<Unit, Unit> MoveDown { get; }
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

        _sortDirectionObservable
            .BindTo(this, vm => vm._sortDirection)
            .DisposeWith(_disposables);

        var sortIndexObservable = this.WhenAnyValue(vm => vm.SortIndex);
        var canExecuteUp = Observable.CombineLatest(
            sortIndexObservable,
            _sortDirectionObservable,
            _lastIndexObservable,
            (sortIndex, sortDirection, lastIndex) =>
                sortDirection == ListSortDirection.Ascending ? sortIndex > 0 : sortIndex < lastIndex
        );

        var canExecuteDown = Observable.CombineLatest(
                sortIndexObservable,
                _sortDirectionObservable,
                _lastIndexObservable,
                (sortIndex, sortDirection, lastIndex) =>
                    sortDirection == ListSortDirection.Ascending ? sortIndex < lastIndex : sortIndex > 0
            );

        MoveUp = ReactiveUI.ReactiveCommand.Create(() =>
            {
                var delta = _sortDirection == ListSortDirection.Ascending ? -1 : +1;
                commandSubject.OnNext(new MoveUpDownCommandPayload(this, delta));
            },
            canExecuteUp
        );

        MoveDown = ReactiveUI.ReactiveCommand.Create(() =>
            {
                var delta = _sortDirection == ListSortDirection.Ascending ? +1 : -1;
                commandSubject.OnNext(new MoveUpDownCommandPayload(this, delta));
            },
            canExecuteDown
        );
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
