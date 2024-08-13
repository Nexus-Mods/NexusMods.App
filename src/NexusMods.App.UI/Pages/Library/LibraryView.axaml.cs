using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.Controls;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Library;

[UsedImplicitly]
public partial class LibraryView : ReactiveUserControl<ILibraryViewModel>
{
    public LibraryView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Source, view => view.TreeDataGrid.Source)
                .DisposeWith(disposables);

            Observable.FromEventHandler<TreeDataGridRowEventArgs>(
                addHandler: handler => TreeDataGrid.RowPrepared += handler,
                removeHandler: handler => TreeDataGrid.RowPrepared -= handler
            )
                .Select(tuple =>
                {
                    var (_, args) = tuple;
                    var row = args.Row;

                    var model = row.Model;
                    if (model is not Node node) return null;
                    node.Activate();

                    // NOTE(erri120): this assumes that the row will not be assigned
                    // a different model between now and when it gets detached
                    return Observable.FromEventHandler<VisualTreeAttachmentEventArgs>(
                        addHandler: handler => row.DetachedFromVisualTree += handler,
                        removeHandler: handler => row.DetachedFromVisualTree -= handler
                    ).Select(node, static (_, node) => node);
                })
                .Where(static x => x is not null)
                .Select(x => x!)
                .Merge()
                .Subscribe(static node => node.Deactivate())
                .AddTo(disposables);
        });
    }
}

