using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Pages.Settings;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Dialog;

/// <summary>
/// ViewModel instances used in design mode
/// </summary>
internal static class DesignDataContexts
{
    public static CustomContentExampleViewModel CustomContentExampleDesignViewModel { get; } = new("Custom Text");

    public static InputDialogViewModel InputDialogDesignViewModel { get; } = new
    (
        new DialogBaseModel(
            title: "Input Dialog",
            buttonDefinitions:
            [
                DialogStandardButtons.Ok,
                DialogStandardButtons.Cancel,
            ],
            text: "Please enter your input:",
            icon: IconValues.AccountCog,
            dialogWindowSize: DialogWindowSize.Medium
        ),
        inputLabel: "Input",
        inputWatermark: "Type here..."
    );
    
    public static MessageDialogViewModel MessageDialogViewModelIcon { get; } = new(
        new DialogBaseModel(
            title: "Delete this mod?",
            buttonDefinitions:
            [
                DialogStandardButtons.Yes,
                DialogStandardButtons.No,
            ],
            text: "Deleting this mod will remove it from all collections. This action cannot be undone.",
            icon: IconValues.PictogramSettings,
            dialogWindowSize: DialogWindowSize.Medium
        )
    );

    // public static MarkdownRendererViewModel MarkdownRendererDesignViewModel { get; } = new MarkdownRendererViewModel
    // {
    //     Contents = """
    //         ```
    //         System.AggregateException: One or more errors occurred. (Entity `EId:4000000000015BF` doesn't have attribute NexusMods.Loadouts.DiskStateEntry/Hash)
    //          ---> System.Collections.Generic.KeyNotFoundException: Entity `EId:4000000000015BF` doesn't have attribute NexusMods.Loadouts.DiskStateEntry/Hash
    //            at NexusMods.MnemonicDB.Abstractions.Attributes.ScalarAttribute`3.ThrowKeyNotfoundException(EntityId entityId)
    //            at NexusMods.MnemonicDB.Abstractions.Attributes.ScalarAttribute`3.Get[T](T entity)
    //            at NexusMods.Abstractions.Loadouts.DiskStateEntry.ReadOnly.get_Hash() in C:\work\NexusMods.App\src\NexusMods.Abstractions.Loadouts\obj\Debug\net9.0\NexusMods.MnemonicDB.SourceGenerator\NexusMods.MnemonicDB.SourceGenerator.ModelGenerator\NexusMods_Abstractions_Loadouts_DiskStateEntry.Generated.cs:line 333
    //            at NexusMods.Abstractions.Loadouts.Synchronizers.ALoadoutSynchronizer.DiskStateToPathPartPair[T](T entries)+MoveNext() in C:\work\NexusMods.App\src\NexusMods.Abstractions.Loadouts.Synchronizers\ALoadoutSynchronizer.cs:line 371
    //            at NexusMods.Abstractions.Loadouts.Synchronizers.ALoadoutSynchronizer.MergeStates(IEnumerable`1 currentState, IEnumerable`1 previousTree, Dictionary`2 loadoutItems) in C:\work\NexusMods.App\src\NexusMods.Abstractions.Loadouts.Synchronizers\ALoadoutSynchronizer.cs:line 312
    //            at NexusMods.Abstractions.Loadouts.Synchronizers.ALoadoutSynchronizer.BuildSyncTree(IEnumerable`1 currentState, IEnumerable`1 previousState, ReadOnly loadout) in C:\work\NexusMods.App\src\NexusMods.Abstractions.Loadouts.Synchronizers\ALoadoutSynchronizer.cs:line 287
    //            at NexusMods.Abstractions.Loadouts.Synchronizers.ALoadoutSynchronizer.ShouldSynchronize(ReadOnly loadout, Entities`1 previousDiskState, Entities`1 lastScannedDiskState) in C:\work\NexusMods.App\src\NexusMods.Abstractions.Loadouts.Synchronizers\ALoadoutSynchronizer.cs:line 1050
    //            at NexusMods.DataModel.Synchronizer.SynchronizerService.<>c__DisplayClass10_0.<GetShouldSynchronize>b__0(IJobContext`1 ctx) in C:\work\NexusMods.App\src\NexusMods.DataModel\Synchronizer\SynchronizerService.cs:line 77
    //            at NexusMods.Jobs.JobContext`2.Start() in C:\work\NexusMods.App\src\NexusMods.Jobs\JobContext.cs:line 42
    //            at NexusMods.DataModel.Synchronizer.SynchronizerService.<>c__DisplayClass21_0.<<CreateStatusObservable>b__5>d.MoveNext() in C:\work\NexusMods.App\src\NexusMods.DataModel\Synchronizer\SynchronizerService.cs:line 241
    //         --- End of stack trace from previous location ---
    //            at NexusMods.DataModel.Synchronizer.SynchronizerService.<>c__DisplayClass21_0.<<CreateStatusObservable>b__4>d.MoveNext() in C:\work\NexusMods.App\src\NexusMods.DataModel\Synchronizer\SynchronizerService.cs:line 238
    //         --- End of stack trace from previous location ---
    //            at R3.SelectAwait`2.SelectAwaitThrottleFirstLast.OnNextAsync(T value, CancellationToken cancellationToken, Boolean configureAwait)
    //            at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(Int16 token)
    //            at R3.AwaitOperationThrottleFirstLastObserver`1.RunQueueWorker()
    //            --- End of inner exception stack trace ---
    //         ``` 
    //         """,
    // };
    //
    // /*
    //  * The following message box examples are used for design purposes only. They are not intended to be used in production code.
    //  * https://www.figma.com/design/RGRSmIC4KoVlIosQB5YmQY/%F0%9F%93%B1%F0%9F%A7%B1-App-components?m=auto&node-id=2-1912
    //  */
    //
    // public static DialogViewModel DialogDesignViewModelExampleSmall { get; } = new(
    //     "Delete this mod?",
    //     [
    //         DialogStandardButtons.Cancel,
    //         new DialogButtonDefinition(
    //             "Yes, delete",
    //             ButtonDefinitionId.From("yes-delete"),
    //             ButtonAction.Accept,
    //             ButtonStyling.Destructive
    //         )
    //     ],
    //     "Deleting this mod will remove it from all collections. This action cannot be undone."
    // );
    //
    // public static DialogViewModel DialogDesignViewModelExampleMedium { get; } = new(
    //     "Delete this mod?",
    //     [
    //         DialogStandardButtons.Cancel,
    //         new DialogButtonDefinition(
    //             "Find out more",
    //             ButtonDefinitionId.From("find-out-more")
    //         ),
    //         new DialogButtonDefinition(
    //             "Get Premium",
    //             ButtonDefinitionId.From("get-premium"),
    //             ButtonAction.Accept,
    //             ButtonStyling.Premium
    //         )
    //     ],
    //     "Deleting this mod will remove it from all collections. This action cannot be undone.",
    //     "Get Premium",
    //     IconValues.Premium
    // );
    //

    //
    // public static DialogViewModel DialogCustomDesignViewModel { get; } = new(
    //     "Delete this mod?",
    //     [DialogStandardButtons.Ok, DialogStandardButtons.Cancel],
    //     "Deleting this mod will remove it from all collections. This action cannot be undone.",
    //     null,
    //     IconValues.PictogramSettings,
    //     DialogWindowSize.Medium,
    //     null,
    //     CustomContentExampleDesignViewModel
    // );
    //
    // public static DialogViewModel DialogCustomNoButtonsDesignViewModel { get; } = new(
    //     "Delete this mod?",
    //     [],
    //     "Deleting this mod will remove it from all collections. This action cannot be undone.",
    //     "Heading",
    //     IconValues.PictogramSettings,
    //     DialogWindowSize.Large,
    //     null,
    //     CustomContentExampleDesignViewModel
    // );
    //
    // public static DialogViewModel DialogMarkdownDesignViewModel { get; } = new(
    //     "Title",
    //     [DialogStandardButtons.Ok, DialogStandardButtons.Yes, DialogStandardButtons.Cancel],
    //     "",
    //     "  ",
    //     null,
    //     DialogWindowSize.Medium,
    //     MarkdownRendererDesignViewModel
    // );
    //
    // public static DialogViewModel DialogAllDesignViewModel { get; } = new(
    //     "Title",
    //     [DialogStandardButtons.Ok, DialogStandardButtons.Yes, DialogStandardButtons.Cancel],
    //     "This is a design viewmodel with all options.",
    //     "Heading",
    //     IconValues.PictogramSettings,
    //     DialogWindowSize.Medium,
    //     MarkdownRendererDesignViewModel,
    //     CustomContentExampleDesignViewModel
    // );
}
