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
            serialDisposable.DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel)
                .SubscribeWithErrorLogging(vm =>
                {
                    WorkspaceCanvas.Children.Clear();

                    if (vm is null)
                    {
                        serialDisposable.Disposable = null;
                        return;
                    }

                    vm.Arrange(_lastSize);

                    var panelsObservable = vm.Panels
                        .ToObservableChangeSet()
                        .Transform(Control (panelViewModel) => new PanelView
                        {
                            ViewModel = panelViewModel,
                        });

                    var resizersObservable = vm.Resizers
                        .ToObservableChangeSet()
                        .Transform(resizerViewModel =>
                        {
                            Control control = new PanelResizerView
                            {
                                ViewModel = resizerViewModel,
                            };

                            // NOTE(erri120): highest number is drawn last
                            control.SetValue(ZIndexProperty, 999);
                            return control;
                        });

                    serialDisposable.Disposable = panelsObservable
                        .Merge(resizersObservable)
                        .RemoveIndex()
                        .Adapt(new ListAdapter<Control>(WorkspaceCanvas.Children))
                        .SubscribeWithErrorLogging();
                })
                .DisposeWith(disposables);
        });
    }

    private Size _lastSize;
    protected override Size ArrangeOverride(Size finalSize)
    {
        _lastSize = finalSize;
        ViewModel?.Arrange(finalSize);

        return base.ArrangeOverride(finalSize);
    }
}
