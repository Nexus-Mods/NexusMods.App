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
                    new DialogButtonDefinition(
                        "Secondary",
                        ButtonDefinitionId.From("cancel"),
                        ButtonAction.Reject
                    ),
                    new DialogButtonDefinition(
                        "Primary",
                        ButtonDefinitionId.From("primary"),
                        ButtonAction.Accept,
                        ButtonStyling.Primary
                    )
                ],
                customViewModel,
                DialogWindowSize.Medium
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
                DialogStandardButtons.Cancel,
                new DialogButtonDefinition(
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
                DialogStandardButtons.Cancel,
                new DialogButtonDefinition(
                    "Find out more",
                    ButtonDefinitionId.From("find-out-more")
                ),
                new DialogButtonDefinition(
                    "Get Premium",
                    ButtonDefinitionId.From("get-premium"),
                    ButtonAction.Accept,
                    ButtonStyling.Premium
                )
            ],
            IconValues.Premium,
            DialogWindowSize.Medium,
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
                DialogStandardButtons.Cancel,
                new DialogButtonDefinition(
                    "This is great",
                    ButtonDefinitionId.From("read-markdown"),
                    ButtonAction.Accept,
                    ButtonStyling.Info
                )
            ],
            IconValues.PictogramSettings,
            DialogWindowSize.Medium,
            markdownRendererViewModel
        );
        
        ShowModal(dialog);
    }

    private async void ShowModalExampleCustom_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel is null) return;

            // create custom content viewmodel
            var customViewModel = new CustomContentViewModel("This is more lovely text");

            // create wrapper dialog around the custom content 
            var dialog = DialogFactory.CreateMessageBox(
                "Custom Dialog", [
                    new DialogButtonDefinition(
                        "Secondary",
                        ButtonDefinitionId.From("cancel"),
                        ButtonAction.Reject
                    ),
                    new DialogButtonDefinition(
                        "Primary",
                        ButtonDefinitionId.From("primary"),
                        ButtonAction.Accept,
                        ButtonStyling.Primary
                    )
                ],
                customViewModel,
                DialogWindowSize.Medium
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

    private void ShowModalUnhandledException_OnClick(object? sender, RoutedEventArgs e)
    {
        var markdownRendererViewModel = ViewModel!.ServiceProvider.GetRequiredService<IMarkdownRendererViewModel>();
        markdownRendererViewModel.Contents = """
            ```
            System.AggregateException: One or more errors occurred. (Entity `EId:4000000000015BF` doesn't have attribute NexusMods.Loadouts.DiskStateEntry/Hash)
             ---> System.Collections.Generic.KeyNotFoundException: Entity `EId:4000000000015BF` doesn't have attribute NexusMods.Loadouts.DiskStateEntry/Hash
               at NexusMods.MnemonicDB.Abstractions.Attributes.ScalarAttribute`3.ThrowKeyNotfoundException(EntityId entityId)
               at NexusMods.MnemonicDB.Abstractions.Attributes.ScalarAttribute`3.Get[T](T entity)
               at NexusMods.Abstractions.Loadouts.DiskStateEntry.ReadOnly.get_Hash() in C:\work\NexusMods.App\src\NexusMods.Abstractions.Loadouts\obj\Debug\net9.0\NexusMods.MnemonicDB.SourceGenerator\NexusMods.MnemonicDB.SourceGenerator.ModelGenerator\NexusMods_Abstractions_Loadouts_DiskStateEntry.Generated.cs:line 333
               at NexusMods.Abstractions.Loadouts.Synchronizers.ALoadoutSynchronizer.DiskStateToPathPartPair[T](T entries)+MoveNext() in C:\work\NexusMods.App\src\NexusMods.Abstractions.Loadouts.Synchronizers\ALoadoutSynchronizer.cs:line 371
               at NexusMods.Abstractions.Loadouts.Synchronizers.ALoadoutSynchronizer.MergeStates(IEnumerable`1 currentState, IEnumerable`1 previousTree, Dictionary`2 loadoutItems) in C:\work\NexusMods.App\src\NexusMods.Abstractions.Loadouts.Synchronizers\ALoadoutSynchronizer.cs:line 312
               at NexusMods.Abstractions.Loadouts.Synchronizers.ALoadoutSynchronizer.BuildSyncTree(IEnumerable`1 currentState, IEnumerable`1 previousState, ReadOnly loadout) in C:\work\NexusMods.App\src\NexusMods.Abstractions.Loadouts.Synchronizers\ALoadoutSynchronizer.cs:line 287
               at NexusMods.Abstractions.Loadouts.Synchronizers.ALoadoutSynchronizer.ShouldSynchronize(ReadOnly loadout, Entities`1 previousDiskState, Entities`1 lastScannedDiskState) in C:\work\NexusMods.App\src\NexusMods.Abstractions.Loadouts.Synchronizers\ALoadoutSynchronizer.cs:line 1050
               at NexusMods.DataModel.Synchronizer.SynchronizerService.<>c__DisplayClass10_0.<GetShouldSynchronize>b__0(IJobContext`1 ctx) in C:\work\NexusMods.App\src\NexusMods.DataModel\Synchronizer\SynchronizerService.cs:line 77
               at NexusMods.Jobs.JobContext`2.Start() in C:\work\NexusMods.App\src\NexusMods.Jobs\JobContext.cs:line 42
               at NexusMods.DataModel.Synchronizer.SynchronizerService.<>c__DisplayClass21_0.<<CreateStatusObservable>b__5>d.MoveNext() in C:\work\NexusMods.App\src\NexusMods.DataModel\Synchronizer\SynchronizerService.cs:line 241
            --- End of stack trace from previous location ---
               at NexusMods.DataModel.Synchronizer.SynchronizerService.<>c__DisplayClass21_0.<<CreateStatusObservable>b__4>d.MoveNext() in C:\work\NexusMods.App\src\NexusMods.DataModel\Synchronizer\SynchronizerService.cs:line 238
            --- End of stack trace from previous location ---
               at R3.SelectAwait`2.SelectAwaitThrottleFirstLast.OnNextAsync(T value, CancellationToken cancellationToken, Boolean configureAwait)
               at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(Int16 token)
               at R3.AwaitOperationThrottleFirstLastObserver`1.RunQueueWorker()
               --- End of inner exception stack trace ---
            ``` 
            """;
        
        var dialog = DialogFactory.CreateMessageBox(
            "Unhandled Exception",
            "This is an example of a markdown message box.",
            "",
            [DialogStandardButtons.Ok],
            IconValues.Warning,
            DialogWindowSize.Medium,
            markdownRendererViewModel
        );
        
        ShowModal(dialog);
    }

    private void ShowModalOk_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = DialogFactory.CreateMessageBox(
            "OK",
            "This is an OK Cancel message box.",
            [DialogStandardButtons.Ok]
        );
        
        ShowModal(dialog);
    }

    private void ShowModelessOk_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = DialogFactory.CreateMessageBox(
            "OK",
            "This is an OK Cancel message box.",
            [DialogStandardButtons.Ok]
        );
        
        ShowModeless(dialog);
    }
}
