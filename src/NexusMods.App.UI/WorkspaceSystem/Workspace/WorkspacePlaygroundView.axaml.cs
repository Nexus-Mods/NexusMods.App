using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
public partial class WorkspacePlaygroundView : ReactiveUserControl<WorkspacePlaygroundViewModel>
{
    public WorkspacePlaygroundView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ViewModelViewHost.ViewModel = ViewModel?.WorkspaceViewModel;

            Setup(disposables, TopLeftTextBlock, view => view.ViewModel!.HasTopLeftPanel, view => view.TopLeftButton, vm => vm.ToggleTopLeftPanel);
            Setup(disposables, TopRightTextBlock, view => view.ViewModel!.HasTopRightPanel, view => view.TopRightButton, vm => vm.ToggleTopRightPanel);
            Setup(disposables, BottomLeftTextBlock, view => view.ViewModel!.HasBottomLeftPanel, view => view.BottomLeftButton, vm => vm.ToggleBottomLeftPanel);
            Setup(disposables, BottomRightTextBlock, view => view.ViewModel!.HasBottomRightPanel, view => view.BottomRightButton, vm => vm.ToggleBottomRightPanel);
        });
    }

    private void Setup(
        CompositeDisposable disposables,
        TextBlock textBlock,
        Expression<Func<WorkspacePlaygroundView, bool>> hasPanelExpression,
        Expression<Func<WorkspacePlaygroundView, Button>> toggleButtonExpression,
        Expression<Func<WorkspacePlaygroundViewModel, ReactiveCommand<Unit, Unit>?>> toggleButtonCommandExpression)
    {
        this.WhenAnyValue(hasPanelExpression)
            .SubscribeWithErrorLogging(value => textBlock.Text = GetText(value))
            .DisposeWith(disposables);

        this.BindCommand(ViewModel, toggleButtonCommandExpression, toggleButtonExpression)
            .DisposeWith(disposables);
    }

    private static string GetText(bool value) => value ? "Remove Panel" : "Add Panel";
}

