using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
public partial class PanelTabHeaderView : ReactiveUserControl<IPanelTabHeaderViewModel>
{
    public PanelTabHeaderView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel!.Icon)
                .SubscribeWithErrorLogging(icon =>
                {
                    IconImage.Source = icon;

                    var size = icon?.Size ?? new Size(0, 0);
                    IconImage.Width = size.Width;
                    IconImage.Height = size.Height;
                })
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Title, view => view.TitleTextBlock.Text)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.Title)
                .SubscribeWithErrorLogging(title => ToolTip.SetTip(this, title))
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.CloseTabCommand, view => view.CloseTabButton)
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.ViewModel!.IsSelected)
                .SubscribeWithErrorLogging(isSelected =>
                {
                    if (isSelected) Container.Classes.Add("Selected");
                    else Container.Classes.Remove("Selected");
                })
                .DisposeWith(disposables);
        });
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel is null) return;
        ViewModel.IsSelected = true;
    }
}

