using System.Reactive.Disposables;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.MessageBox.Enums;
using NexusMods.App.UI.Windows;
using ReactiveUI;
using R3;

namespace NexusMods.App.UI.Pages.DebugControls;

public partial class DebugControlsPageView : ReactiveUserControl<IDebugControlsPageViewModel>
{
    public DebugControlsPageView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel, vm => vm.GenerateUnhandledException, v => v.GenerateUnhandledException.Command)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel, vm => vm.MarkdownRenderer, v => v.MarkdownRendererViewModelViewHost.ViewModel)
                    .DisposeWith(disposables);
            }
        );
    }


    private async void ShowModal(object? sender, RoutedEventArgs e, string title, string message, ButtonEnum buttonType)
    {
        if (ViewModel is null) return;

        var result = await ViewModel.WindowManager.ShowModalAsync(title, message, buttonType);
        Console.WriteLine($@"{buttonType} result: {result}");
    }
        
        private async void ShowModalOk_OnClick(object? sender, RoutedEventArgs e) =>
            ShowModal(sender, e, "Test Modal", "This is an Ok modal", ButtonEnum.Ok);
        
        private async void ShowModalOkCancel_OnClick(object? sender, RoutedEventArgs e) =>
            ShowModal(sender, e, "Test Modal", "This is an OkCancel modal", ButtonEnum.OkCancel);
        
        private async void ShowModalYesNo_OnClick(object? sender, RoutedEventArgs e) =>
            ShowModal(sender, e, "Test Modal", "This is a YesNo modal", ButtonEnum.YesNo);
        
        private async void ShowModalYesNoCancel_OnClick(object? sender, RoutedEventArgs e) =>
            ShowModal(sender, e, "Test Modal", "This is a YesNoCancel modal", ButtonEnum.YesNoCancel);
        
        private async void ShowModalOkAbort_OnClick(object? sender, RoutedEventArgs e) =>
            ShowModal(sender, e, "Test Modal", "This is an OkAbort modal", ButtonEnum.OkAbort);
        
        private async void ShowModalYesNoAbort_OnClick(object? sender, RoutedEventArgs e) =>
            ShowModal(sender, e, "Test Modal", "This is a YesNoAbort modal", ButtonEnum.YesNoAbort);

        private async void ShowModeless(object? sender, RoutedEventArgs e, string title, string message, ButtonEnum buttonType)
        {
            if (ViewModel is null) return;

            var result = await ViewModel.WindowManager.ShowModelessAsync(title, message, buttonType);
            Console.WriteLine($@"{buttonType} result: {result}");
        }
        
        private void ShowModelessOk_OnClick(object? sender, RoutedEventArgs e) =>
            ShowModeless(sender, e, "Test Modeless", "This is an Ok modeless", ButtonEnum.Ok);

        private void ShowModelessYesNo_OnClick(object? sender, RoutedEventArgs e) =>
            ShowModeless(sender, e, "Test Modeless", "This is a YesNo modeless", ButtonEnum.YesNo);

        private void ShowModelessOkCancel_OnClick(object? sender, RoutedEventArgs e) =>
            ShowModeless(sender, e, "Test Modeless", "This is an OkCancel modeless", ButtonEnum.OkCancel);

        private void ShowModelessOkAbort_OnClick(object? sender, RoutedEventArgs e) =>
            ShowModeless(sender, e, "Test Modeless", "This is an OkAbort modeless", ButtonEnum.OkAbort);

        private void ShowModelessYesNoCancel_OnClick(object? sender, RoutedEventArgs e) =>
            ShowModeless(sender, e, "Test Modeless", "This is a YesNoCancel modeless", ButtonEnum.YesNoCancel);

        private void ShowModelessYesNoAbort_OnClick(object? sender, RoutedEventArgs e) =>
            ShowModeless(sender, e, "Test Modeless", "This is a YesNoAbort modeless", ButtonEnum.YesNoAbort);
}
