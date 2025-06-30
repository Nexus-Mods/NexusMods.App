using System.Reactive.Disposables;
using Avalonia.Controls.Chrome;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Windows;
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

    private void ShowModalYesNo_OnClick(object? sender, RoutedEventArgs e)
    {
        // ShowModal(
        //     DialogFactory.CreateYesNoMessageBox(
        //         "Yes No",
        //         "This is a Yes No message box."
        //     )
        // );
    }

    private void ShowModelessYesNo_OnClick(object? sender, RoutedEventArgs e)
    {
        // ShowModeless(
        //     DialogFactory.CreateYesNoMessageBox(
        //         "Yes No",
        //         "This is a Yes No message box."
        //     )
        // );
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

    private async void ShowModalExampleCustom_OnClick(object? sender, RoutedEventArgs e)
    {
        // if (ViewModel is null) return;
        //
        // // create custom content viewmodel
        // var customViewModel = new CustomContentExampleViewModel("This is more lovely text");
        //
        // // create wrapper dialog around the custom content 
        // var dialog = DialogFactory.CreateMessageDialog(
        //     title: "Custom Dialog",
        //     text: "Some text can be here",
        //     buttonDefinitions:
        //     [
        //         new DialogButtonDefinition(
        //             "Secondary",
        //             ButtonDefinitionId.From("cancel"),
        //             ButtonAction.Reject
        //         ),
        //         new DialogButtonDefinition(
        //             "Primary",
        //             ButtonDefinitionId.From("primary"),
        //             ButtonAction.Accept,
        //             ButtonStyling.Primary
        //         )
        //     ],
        //     dialogWindowSize: DialogWindowSize.Medium,
        //     contentViewModel: customViewModel
        // );
        //
        // // tell windowmanager to show it
        // var result = await ViewModel.WindowManager.ShowDialog(dialog, DialogWindowType.Modal);
        //
        // // check viewmodel properties when dialog has been closed
        // Console.WriteLine($@"result: {result}");
        // Console.WriteLine($@"DontAskAgain: {customViewModel.DontAskAgain}");
        // Console.WriteLine($@"ShouldEndorseDownloadedMods: {customViewModel.ShouldEndorseDownloadedMods}");
    }

    private void ShowModalUnhandledException_OnClick(object? sender, RoutedEventArgs e)
    {
        var markdownRendererViewModel = ViewModel!.ServiceProvider.GetRequiredService<IMarkdownRendererViewModel>();

        markdownRendererViewModel.Contents = """
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
            """;

        // var dialog = DialogFactory.CreateMessageDialog(
        //     title: "Unhandled Exception",
        //     text: "This is an example of a markdown message box.",
        //     buttonDefinitions: [DialogStandardButtons.Ok],
        //     icon: IconValues.Warning,
        //     dialogWindowSize: DialogWindowSize.Medium,
        //     markdownRenderer: markdownRendererViewModel
        // );
        //
        // ShowModal(dialog);
    }

    private async void ShowModalAllControls_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;

        // create custom content viewmodel
        var customViewModel = new CustomContentExampleViewModel("This is more lovely text");

        // create markdown viewmodel
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

        // create wrapper dialog around the custom content 
        // var dialog = DialogFactory.CreateMessageDialog(
        //     title: "Custom Dialog",
        //     text: "Some text can be here",
        //     heading: "And even a heading",
        //     buttonDefinitions: [
        //         new DialogButtonDefinition(
        //             "Secondary",
        //             ButtonDefinitionId.From("cancel"),
        //             ButtonAction.Reject
        //         ),
        //         new DialogButtonDefinition(
        //             "Primary",
        //             ButtonDefinitionId.From("primary"),
        //             ButtonAction.Accept,
        //             ButtonStyling.Primary
        //         )
        //     ],
        //     icon: IconValues.PictogramHealth,
        //     dialogWindowSize: DialogWindowSize.Medium,
        //     markdownRenderer: markdownRendererViewModel,
        //     contentViewModel: customViewModel
        // );
        //
        // // tell windowmanager to show it
        // var result = await ViewModel.WindowManager.ShowDialog(dialog, DialogWindowType.Modal);
        //
        // // check viewmodel properties when dialog has been closed
        // Console.WriteLine($@"result: {result}");
        // Console.WriteLine($@"DontAskAgain: {customViewModel.DontAskAgain}");
        // Console.WriteLine($@"ShouldEndorseDownloadedMods: {customViewModel.ShouldEndorseDownloadedMods}");
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
}
