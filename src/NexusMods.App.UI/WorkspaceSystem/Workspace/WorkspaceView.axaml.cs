using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using NexusMods.App.UI.Extensions;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
public partial class WorkspaceView : ReactiveUserControl<IWorkspaceViewModel>
{
    public WorkspaceView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            Debug.Assert(WorkspaceCanvas.Children.Count == 0);
            var serialDisposable = new SerialDisposable();

            this.WhenAnyValue(view => view.ViewModel!.Panels)
                .Do(panels =>
                {
                    serialDisposable.Disposable = panels
                        .ToObservableChangeSet()
                        .Transform(CreateView)
                        .Adapt(new ListAdapter<Control>(WorkspaceCanvas.Children))
                        .Subscribe();
                })
                .Subscribe()
                .DisposeWith(disposables);

            serialDisposable.DisposeWith(disposables);
        });
    }

    private static Control CreateView(IPanelViewModel panelViewModel)
    {
        return new PanelView
        {
            ViewModel = panelViewModel
        };
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        ViewModel?.ArrangePanels(finalSize);
        return base.ArrangeOverride(finalSize);
    }
}
