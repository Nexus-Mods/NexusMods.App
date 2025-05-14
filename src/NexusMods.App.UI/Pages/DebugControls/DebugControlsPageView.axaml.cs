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
                customViewModel,
                MessageBoxSize.Medium
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
                "OK Cancel",
                "This is an OK Cancel message box."
            )
        );
    }

    private void ShowModelessOkCancel_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowModeless(
            DialogFactory.CreateOkCancelMessageBox(
                "OK Cancel",
                "This is an OK Cancel message box."
            )
        );
    }

    private void ShowModalYesNo_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowModal(
            DialogFactory.CreateYesNoMessageBox(
                "Yes No",
                "This is a Yes No message box."
            )
        );
    }

    private void ShowModelessYesNo_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowModeless(
            DialogFactory.CreateYesNoMessageBox(
                "Yes No",
                "This is a Yes No message box."
            )
        );
    }

    private void ShowModalExampleSmall_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = DialogFactory.CreateMessageBox("Delete this mod?",
            "Deleting this mod will remove it from all collections. This action cannot be undone.",
            [
                MessageBoxStandardButtons.Cancel,
                new MessageBoxButtonDefinition(
                    "Yes, delete",
                    ButtonDefinitionId.From("yes-delete"),
                    ButtonAction.Accept,
                    ButtonStyling.Destructive
                ),
            ]
        );
        
        ShowModal(dialog);
    }

    private void ShowModalExampleMedium_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = DialogFactory.CreateMessageBox(
            "Get Premium",
            "Download entire collections at full speed with one click and without leaving the app.",
            "Get Premium for one-click collection installs",
            [
                MessageBoxStandardButtons.Cancel,
                new MessageBoxButtonDefinition(
                    "Find out more",
                    ButtonDefinitionId.From("find-out-more")
                ),
                new MessageBoxButtonDefinition(
                    "Get Premium",
                    ButtonDefinitionId.From("get-premium"),
                    ButtonAction.Accept,
                    ButtonStyling.Premium
                )
            ],
            IconValues.Premium,
            MessageBoxSize.Medium,
            null
        );
        
        ShowModal(dialog);
    }

    private void ShowModalExampleMarkdown_OnClick(object? sender, RoutedEventArgs e)
    {
        var markdownRendererViewModel = ViewModel!.ServiceProvider.GetRequiredService<IMarkdownRendererViewModel>();
        // markdownRendererViewModel.Contents = """
        //     ## This is a markdown message box
        //     
        //     This is an example of a markdown message box.
        //     
        //     You can use **bold** and *italic* text.
        //     
        //     You can also use [links](https://www.nexusmods.com).
        //     """;
        markdownRendererViewModel.Contents = MarkdownRendererViewModel.DebugText;
        
        var dialog = DialogFactory.CreateMessageBox(
            "Markdown Message Box",
            "This is an example of a markdown message box.",
            "Lovely markdown just below",
            [
                MessageBoxStandardButtons.Cancel,
                new MessageBoxButtonDefinition(
                    "This is great",
                    ButtonDefinitionId.From("read-markdown"),
                    ButtonAction.Accept,
                    ButtonStyling.Info
                )
            ],
            IconValues.PictogramSettings,
            MessageBoxSize.Medium,
            markdownRendererViewModel
        );
        
        ShowModal(dialog);
    }
}
