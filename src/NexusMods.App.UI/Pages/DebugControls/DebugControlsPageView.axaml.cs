using System.Reactive.Disposables;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Windows;
using NexusMods.Icons;
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

    private async void ShowModelessCustom_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel is null) return;

            // create custom content viewmodel
            var customViewModel = new CustomContentViewModel("This is more lovely text");

            // create wrapper dialog around the custom content 
            var dialog = DialogFactory.CreateMessageBox(
                "Custom Dialog", [
                    new MessageBoxButtonDefinition(
                        "Secondary",
                        ButtonDefinitionId.From("cancel"),
                        ButtonAction.Reject
                    ),
                    new MessageBoxButtonDefinition(
                        "Primary",
                        ButtonDefinitionId.From("primary"),
                        ButtonAction.Accept,
                        ButtonStyling.Primary
                    )
                ],
                customViewModel
            );

            // tell windowmanager to show it
            var result = await ViewModel.WindowManager.ShowDialog(dialog, DialogWindowType.Modeless);

            // check viewmodel properties when dialog has been closed
            Console.WriteLine($@"result: {result}");
            Console.WriteLine($@"DontAskAgain: {customViewModel.DontAskAgain}");
            Console.WriteLine($@"ShouldEndorseDownloadedMods: {customViewModel.ShouldEndorseDownloadedMods}");
        }
        catch
        {
            throw; // TODO handle exception
        }
    }

    private async void ShowModal(IDialog<ButtonDefinitionId> dialog)
    {
        if (ViewModel is null) return;

        var result = await ViewModel.WindowManager.ShowDialog(dialog, DialogWindowType.Modal);
        Console.WriteLine($@"result: {result}");
    }

    private async void ShowModeless(IDialog<ButtonDefinitionId> dialog)
    {
        if (ViewModel is null) return;

        var result = await ViewModel.WindowManager.ShowDialog(dialog, DialogWindowType.Modeless);
        Console.WriteLine($@"result: {result}");
    }


    private void ShowModalOkCancel_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowModal(
            DialogFactory.CreateOkCancelMessageBox(
                "OKCancel",
                "Deleting this mod will remove it from all collections. This action cannot be undone."
            )
        );
    }

    private void ShowModelessOkCancel_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowModeless(
            DialogFactory.CreateOkCancelMessageBox(
                "OKCancel",
                "Deleting this mod will remove it from all collections. This action cannot be undone."
            )
        );
    }
}
