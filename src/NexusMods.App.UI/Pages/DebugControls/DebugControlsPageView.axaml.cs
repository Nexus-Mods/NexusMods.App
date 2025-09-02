using System.Reactive.Disposables;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Standard;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.CrossPlatform.Process;
using NexusMods.UI.Sdk.Dialog;
using NexusMods.UI.Sdk.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.DebugControls;

public partial class DebugControlsPageView : ReactiveUserControl<IDebugControlsPageViewModel>
{
    public DebugControlsPageView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
            {
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
                        ButtonDefinitionId.Accept,
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
                        ButtonDefinitionId.Accept,
                        ButtonAction.Accept,
                        ButtonStyling.Primary
                    ),
                ]
            ));
    }

    private async void ShowModalPremium_OnClick(object? sender, RoutedEventArgs e)
    {
        await PremiumDialog.ShowUpdatePremiumDialog(
            ViewModel!.WindowManager,
            ViewModel.ServiceProvider.GetRequiredService<IOSInterop>()
        );
    }
}
