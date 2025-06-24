using System.Reactive.Disposables;
using Avalonia.Controls.Chrome;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Standard;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Windows;
using NexusMods.CrossPlatform.Process;
using NexusMods.UI.Sdk.Icons;
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

    // helper methods to show modal and modeless dialogs

    private async void ShowModal(IDialog dialog)
    {
        if (ViewModel is null) return;

        var result = await ViewModel.WindowManager.ShowDialog(dialog, DialogWindowType.Modal);
        Console.WriteLine($@"result: {result}");
    }

    private async void ShowModeless(IDialog dialog)
    {
        if (ViewModel is null) return;

        var result = await ViewModel.WindowManager.ShowDialog(dialog, DialogWindowType.Modeless);
        Console.WriteLine($@"result: {result}");
    }

    // event handlers for button clicks

    private void ShowModalOkCancel_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowModal(DialogFactory.CreateStandardDialog(
                "Standard dialog",
                new StandardDialogParameters()
                {
                    Text = "This is a standard modal dialog with OK and Cancel buttons.",
                },
                [DialogStandardButtons.Ok, DialogStandardButtons.Cancel]
            )
        );
    }

    private void ShowModelessOkCancel_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowModeless(DialogFactory.CreateStandardDialog(
                "Standard dialog",
                new StandardDialogParameters()
                {
                    Text = "This is a standard modeless dialog with OK and Cancel buttons.",
                },
                [DialogStandardButtons.Ok, DialogStandardButtons.Cancel]
            )
        );
    }

    private void ShowModalExampleMarkdown_OnClick(object? sender, RoutedEventArgs e)
    {
        var markdownRendererViewModel = ViewModel!.ServiceProvider.GetRequiredService<IMarkdownRendererViewModel>();
        markdownRendererViewModel.Contents = MarkdownRendererViewModel.DebugText;

        ShowModal(DialogFactory.CreateStandardDialog(
                "Markdown Message Box",
                new StandardDialogParameters()
                {
                    Text = "This is an example of a markdown message box.",
                    Heading = "Lovely markdown just below",
                    Icon = IconValues.PictogramSettings,
                    Markdown = markdownRendererViewModel,
                },
                buttonDefinitions:
                [
                    DialogStandardButtons.Cancel,
                    new DialogButtonDefinition(
                        "This is great",
                        ButtonDefinitionId.From("read-markdown"),
                        ButtonAction.Accept,
                        ButtonStyling.Info
                    ),
                ]
            )
        );
    }

    private async void ShowModalInput_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowModal(
            DialogFactory.CreateStandardDialog(
                "Name your Collection",
                new StandardDialogParameters()
                {
                    Text = "This is the name that will appear in the left hand menu and on the Collections page.",
                    InputLabel = "Collection name",
                    InputWatermark = "e.g. My Armour Mods",
                },
                [
                    DialogStandardButtons.Cancel,
                    new DialogButtonDefinition(
                        "Create",
                        ButtonDefinitionId.From("create"),
                        ButtonAction.Accept,
                        ButtonStyling.Primary
                    ),
                ]
            ));
    }
   

    private async void ShowModalPremium_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel is null) return;
            
            var dialog = DialogFactory.CreateMessageBox(
                "Go Premium for one-click mod updates",
                """
                No browser, no manual downloads. Premium users also get:
                
                • Download entire collections with one click
                • Uncapped download speeds
                • No Ads for life, even if you unsubscribe after 1 month!
                """,
                "Update all your mods, or individual mods, in one click.",
                [
                    new DialogButtonDefinition(
                        "Update mods manually",
                        ButtonDefinitionId.From("update-manually"),
                        ButtonAction.Reject),
                    new DialogButtonDefinition(
                        "Upgrade to Premium",
                        ButtonDefinitionId.From("upgrade-premium"),
                        ButtonAction.Accept,
                        ButtonStyling.Premium)
                ],
                IconValues.PictogramPremium,
                DialogWindowSize.Medium,
                null,
                null
            );
            
            // tell windowmanager to show it
            var result = await ViewModel.WindowManager.ShowDialog(dialog, DialogWindowType.Modal);

            // check viewmodel properties when dialog has been closed
            Console.WriteLine($@"result: {result}");
        }
        catch
        {
            throw; // TODO handle exception
        }
    }


    private async void ShowModalPremium_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;

        var osInterop = ViewModel.ServiceProvider.GetRequiredService<IOSInterop>();
        var result = await PremiumDialog.ShowUpdatePremiumDialog(ViewModel.WindowManager, osInterop);

        // check viewmodel properties when dialog has been closed
        Console.WriteLine($@"result: {result}");
    }
}
