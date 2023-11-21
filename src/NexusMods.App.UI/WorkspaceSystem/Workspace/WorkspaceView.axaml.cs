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

            this.WhenAnyValue(property1: view => view.ViewModel!.Panels, property2: view => view.ViewModel!.Resizers)
                .Do(tuple =>
                {
                    var (panels, resizers) = tuple;

                    var panelsObservable = panels
                        .ToObservableChangeSet()
                        .Transform(panelViewModel => (Control)new PanelView
                        {
                            ViewModel = panelViewModel
                        });

                    var resizersObservable = resizers
                        .ToObservableChangeSet()
                        .Transform(resizerViewModel =>
                        {
                            var control = (Control)new PanelResizerView
                            {
                                ViewModel = resizerViewModel
                            };

                            // NOTE(erri120): highest number is drawn last
                            control.SetValue(ZIndexProperty, 999);
                            return control;
                        });

                    serialDisposable.Disposable = panelsObservable
                        .Merge(resizersObservable)
                        .Adapt(new ListAdapter<Control>(WorkspaceCanvas.Children))
                        .Subscribe();
                })
                .Subscribe()
                .DisposeWith(disposables);
        });
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        ViewModel?.Arrange(finalSize);
        return base.ArrangeOverride(finalSize);
    }
}
