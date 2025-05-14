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


    private async void ShowModal(IDialog<ButtonDefinitionId> dialog)
    {
        try
        {
            if (ViewModel is null) return;

            // tell windowmanager to show it
            var result = await ViewModel.WindowManager.ShowDialog(dialog, DialogWindowType.Modal);

            Console.WriteLine($@"ShowModal Result: {result}");
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }


    private async void ShowModeless(IDialog<ButtonDefinitionId> dialog)
    {
        try
        {
            if (ViewModel is null) return;

            var result = await ViewModel.WindowManager.ShowDialog(dialog, DialogWindowType.Modeless);
            Console.WriteLine($@"ShowModeless Result: {result}");
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }


    private void ShowModalOk_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = DialogFactory.CreateMessageBox(
            "Test Modal",
            "This is an Ok modal",
            IconValues.PictogramCelebrate,
            [MessageBoxStandardButtons.Ok],
            MessageBoxSize.Small
        );

        ShowModal(dialog);
    }

    private void ShowModalOkCancel_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = DialogFactory.CreateMessageBox(
            "Test Modal",
            "This is an OkCancel modal",
            null,
            [MessageBoxStandardButtons.Ok, MessageBoxStandardButtons.Cancel],
            MessageBoxSize.Medium
        );

        ShowModal(dialog);
    }

    private void ShowModalShowModalDeleteMod_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = DialogFactory.CreateMessageBox(
            "Delete this mod?",
            "Deleting this mod will remove it from all collections. This action cannot be undone.",
            IconValues.Cog,
            [
                MessageBoxStandardButtons.Cancel,
                new MessageBoxButtonDefinition(
                    "Yes, delete",
                    ButtonDefinitionId.From("yes-delete"),
                    ButtonAction.Accept,
                    ButtonStyling.Destructive
                )
            ],
            MessageBoxSize.Small
        );

        ShowModal(dialog
        );
    }

    private void ShowModalInfo_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = DialogFactory.CreateMessageBox("Updating mods",
            "Updating mods installed in multiple collections. Updating will apply to all local collections where the mod is installed.",
            null, [
                new MessageBoxButtonDefinition(
                    "Cancel with icon",
                    ButtonDefinitionId.From("cancel"),
                    ButtonAction.Reject,
                    ButtonStyling.None,
                    IconValues.Warning
                ),
                new MessageBoxButtonDefinition(
                    "Update in 2 collections",
                    ButtonDefinitionId.From("update"),
                    ButtonAction.Accept,
                    ButtonStyling.Info
                )
            ],
            MessageBoxSize.Large
        );

        ShowModal(dialog);
    }

    private void ShowModalPremium_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = DialogFactory.CreateMessageBox("Go Premium",
            "Download entire collections at full speed with one click, no browser, no manual downloads.",
            IconValues.Premium,
            [
                new MessageBoxButtonDefinition(
                    "Cancel",
                    ButtonDefinitionId.From("cancel"),
                    ButtonAction.Reject
                ),
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
            MessageBoxSize.Medium
        );

        ShowModal(dialog);
    }

    private async void ShowMarkdown_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel is null) return;

            // get markdown service and create markdown renderer

            var markdownRendererViewModel = ViewModel.ServiceProvider.GetRequiredService<IMarkdownRendererViewModel>();
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

            var dialog = DialogFactory.CreateMarkdownMessageBox("Something important",
                "An example showing Primary and Secondary buttons",
                IconValues.PictogramCelebrate, [
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
                MessageBoxSize.Large,
                markdownRendererViewModel
            );
            
            // tell windowmanager to show it
            // result isn't used with custom dialog content as the viewmodel properties can be accessed directly 
            var result = await ViewModel.WindowManager.ShowDialog(dialog, DialogWindowType.Modal);
            Console.WriteLine(result);
        }
        catch
        {
            throw; // TODO handle exception
        }
        
    }

    private void ShowModelessPrimary_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = DialogFactory.CreateMessageBox("Something important",
            "An example showing Primary and Secondary buttons",
            null, [
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
            MessageBoxSize.Medium
        );

        ShowModeless(dialog);
    }

    private async void ShowModalCustom_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel is null) return;

            // create custom content viewmodel
            var customViewModel = new CustomContentViewModel("This is more lovely text");

            // create wrapper dialog around the custom content 
            var dialog = DialogFactory.CreateMessageBox(
                "Custom Dialog",
                customViewModel,
                [
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
                MessageBoxSize.Medium
            );

            // tell windowmanager to show it
            // result isn't used with custom dialog content as the viewmodel properties can be accessed directly 
            var result = await ViewModel.WindowManager.ShowDialog(dialog, DialogWindowType.Modal);

            // check viewmodel properties when dialog has been closed
            Console.WriteLine($@"result: {result}");
            Console.WriteLine($@"DontAskAgain: {customViewModel.DontAskAgain}");
            Console.WriteLine($@"ShouldEndorseDownloadedMods: {customViewModel.ShouldEndorseDownloadedMods}");
            Console.WriteLine($@"MySelectedItem: {customViewModel.MySelectedItem}");
        }
        catch
        {
            throw; // TODO handle exception
        }
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
                "Custom Dialog",
                customViewModel,
                [
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
                MessageBoxSize.Medium
            );

            // tell windowmanager to show it
            // result isn't used with custom dialog content as the viewmodel properties can be accessed directly 
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
}
