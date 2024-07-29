using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using ReactiveUI;
using static NexusMods.App.UI.Controls.DataGrid.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGrid;

[UsedImplicitly]
public partial class LoadoutGridView : ReactiveUserControl<ILoadoutGridViewModel>
{
    public LoadoutGridView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.GroupIds.Count)
                .Select(count => count == 0)
                .BindToView(this, view => view.EmptyState.IsActive)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.GroupIds)
                .BindToView(this, view => view.DataGrid.ItemsSource);

            this.WhenAnyValue(view => view.ViewModel!.Columns)
                .GenerateColumns(DataGrid)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.ViewLibraryCommand, view => view.ViewLibraryButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.ViewFilesCommand, view => view.ViewFilesButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.DeleteCommand, view => view.DeleteButton)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.EmptyStateTitle, view => view.EmptyState.Header)
                .DisposeWith(d);

            Observable.FromEventPattern<SelectionChangedEventArgs>(
                    addHandler: handler => DataGrid.SelectionChanged += handler,
                    removeHandler: handler => DataGrid.SelectionChanged -= handler
                )
                .Select(eventPattern => eventPattern.EventArgs)
                .Do(args =>
                {
                    var sourceList = ViewModel?.SelectedGroupIds;
                    if (sourceList is null) return;

                    var added = args.AddedItems.OfType<LoadoutItemGroupId>();
                    var removed = args.RemovedItems.OfType<LoadoutItemGroupId>();

                    sourceList.Edit(list =>
                    {
                        list.Remove(removed);
                        list.AddRange(added);
                    });
                })
                .SubscribeWithErrorLogging()
                .DisposeWith(d);
        });
    }
}

