using System.Reactive.Disposables;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.MessageBox;
using NexusMods.App.UI.MessageBox.Enums;
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


    private async void ShowModal(string title, string message, MessageBoxButtonDefinition[] buttonDefinitions, MessageBoxSize messageBoxSize)
    {
        try
        {
            if (ViewModel is null) return;

            // create new messagebox
            var messageBox = MessageBoxFactory.Create(title, message, buttonDefinitions,
                messageBoxSize
            );

            // tell windowmanager to show it
            var result = await ViewModel.WindowManager.ShowMessageBox(messageBox, MessageBoxWindowType.Modal);

            Console.WriteLine($@"{buttonDefinitions} result: {result}");
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }


    private async void ShowModeless(string title, string message, MessageBoxButtonDefinition[] buttonDefinitions, MessageBoxSize messageBoxSize)
    {
        try
        {
            if (ViewModel is null) return;

            // create new messagebox
            var messageBox = MessageBoxFactory.Create(title, message, buttonDefinitions,
                messageBoxSize
            );

            var result = await ViewModel.WindowManager.ShowMessageBox(messageBox, MessageBoxWindowType.Modeless);
            Console.WriteLine($@"{buttonDefinitions} result: {result}");
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }


    private void ShowModalOk_OnClick(object? sender, RoutedEventArgs e) =>
        ShowModal("Test Modal",
            "This is an Ok modal",
            [MessageBoxStandardButtons.Ok],
            MessageBoxSize.Small
        );

    private void ShowModalOkCancel_OnClick(object? sender, RoutedEventArgs e) =>
        ShowModal("Test Modal",
            "This is an OkCancel modal",
            [MessageBoxStandardButtons.Ok, MessageBoxStandardButtons.Cancel],
            MessageBoxSize.Medium
        );

    private void ShowModalShowModalDeleteMod_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowModal("Delete this mod?",
            "Deleting this mod will remove it from all collections. This action cannot be undone.", [
                MessageBoxStandardButtons.Cancel,
                new MessageBoxButtonDefinition(
                    "Yes, delete",
                    ButtonDefinitionId.From("yes-delete"),
                    null,
                    null,
                    ButtonRole.AcceptRole | ButtonRole.DestructiveRole
                )
            ],
            MessageBoxSize.Small
        );
    }

    private void ShowModelessOk_OnClick(object? sender, RoutedEventArgs e) =>
        ShowModeless("Test Modeless",
            "This is an Ok modeless",
            [MessageBoxStandardButtons.Ok],
            MessageBoxSize.Small
        );

    private void ShowModelessOkCancel_OnClick(object? sender, RoutedEventArgs e) =>
        ShowModeless("Test Modeless",
            "This is an OkCancel modeless",
            [MessageBoxStandardButtons.Ok, MessageBoxStandardButtons.Cancel],
            MessageBoxSize.Small
        );

    private void ShowModalInfo_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowModal("Updating mods",
            "Updating mods installed in multiple collections. Updating will apply to all local collections where the mod is installed.", [
                new MessageBoxButtonDefinition(
                    "Cancel with icon",
                    ButtonDefinitionId.From("cancel"),
                    IconValues.Warning,
                    null,
                    ButtonRole.RejectRole
                ),
                new MessageBoxButtonDefinition(
                    "Update in 2 collections",
                    ButtonDefinitionId.From("update"),
                    null,
                    null,
                    ButtonRole.AcceptRole | ButtonRole.InfoRole
                )
            ],
            MessageBoxSize.Large
        );
    }

    private void ShowModalPremium_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowModal("Go Premium",
            "Download entire collections at full speed with one click, no browser, no manual downloads.", [
                new MessageBoxButtonDefinition(
                    "Cancel",
                    ButtonDefinitionId.From("cancel"),
                    null,
                    null,
                    ButtonRole.RejectRole
                ),
                new MessageBoxButtonDefinition(
                    "Find out more",
                    ButtonDefinitionId.From("find-out-more")
                ),
                new MessageBoxButtonDefinition(
                    "Get Premium",
                    ButtonDefinitionId.From("get-premium"),
                    null,
                    null,
                    ButtonRole.AcceptRole | ButtonRole.PremiumRole
                )
            ],
            MessageBoxSize.Medium
        );
    }
}
