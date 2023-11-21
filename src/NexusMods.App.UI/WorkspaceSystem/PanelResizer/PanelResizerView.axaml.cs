using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public partial class PanelResizerView : ReactiveUserControl<IPanelResizerViewModel>
{
    public PanelResizerView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Do(PopulateFromViewModel)
                .Subscribe()
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.ActualPosition)
                .SubscribeWithErrorLogging(point =>
                {
                    Canvas.SetLeft(this, point.X);
                    Canvas.SetTop(this, point.Y);
                })
                .DisposeWith(disposables);
        });
    }

    private void PopulateFromViewModel(IPanelResizerViewModel viewModel)
    {
        Icon.Classes.Add(viewModel.IsHorizontal ? "DragHorizontal" : "DragVertical");
    }
}

