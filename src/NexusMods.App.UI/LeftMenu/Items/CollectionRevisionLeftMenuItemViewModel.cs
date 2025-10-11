using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Collections;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Dialog;
using NexusMods.UI.Sdk.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public class CollectionRevisionLeftMenuItemViewModel : LeftMenuItemWithRightIconViewModel
{
    private readonly CollectionRevisionMetadataId _collectionRevisionMetadataId;
    private readonly CollectionDownloader _collectionDownloader;
    private readonly IWindowNotificationService _notificationService;
    private readonly IWindowManager _windowManager;
    private readonly IConnection _connection;

    public CollectionRevisionLeftMenuItemViewModel(
        IWorkspaceController workspaceController,
        WorkspaceId workspaceId,
        PageData pageData,
        CollectionRevisionMetadata.ReadOnly revision,
        IServiceProvider serviceProvider) : base(workspaceController, workspaceId, pageData)
    {
        _collectionRevisionMetadataId = revision;
        _collectionDownloader = serviceProvider.GetRequiredService<CollectionDownloader>();
        _notificationService = serviceProvider.GetRequiredService<IWindowNotificationService>();
        _windowManager = serviceProvider.GetRequiredService<IWindowManager>();
        _connection = serviceProvider.GetRequiredService<IConnection>();

        AdditionalContextMenuItems =
        [
            CreateDeleteContextMenuItem(),
        ];
    }

    private IContextMenuItem CreateDeleteContextMenuItem()
    {
        var deleteCommand = CreateDeleteCommand();
        
        return new ContextMenuItem
        {
            Header = Language.CollectionDownloadView_Menu_DeleteCollectionRevision,
            Icon = IconValues.DeleteOutline,
            Command = deleteCommand,
            IsVisible = true,
            Styling = ContextMenuItemStyling.Critical,
        };
    }
    
    private ReactiveCommand<Unit, Unit> CreateDeleteCommand()
    {
        // Non-installed collection revisions can always be deleted
        var canExecute = Observable.Return(true);

        return ReactiveCommand.CreateFromTask(async () =>
        {
            var confirmed = await ShowDeleteConfirmationDialogAsync();
            if (!confirmed)
                return;

            try
            {
                await _collectionDownloader.DeleteRevision(_collectionRevisionMetadataId);
                _notificationService.ShowToast(
                    Language.ToastNotification_Collection_removed,
                    ToastNotificationVariant.Success
                );
            }
            catch (Exception ex)
            {
                _notificationService.ShowToast(
                    $"Failed to delete collection: {ex.Message}",
                    ToastNotificationVariant.Failure
                );
            }
        }, canExecute: canExecute);
    }

    private async Task<bool> ShowDeleteConfirmationDialogAsync()
    {
        var db = _connection.Db;
        var revision = CollectionRevisionMetadata.Load(db, _collectionRevisionMetadataId);
        var collectionName = revision.Collection.Name;

        var dialog = DialogFactory.CreateStandardDialog(
            title: Language.Loadout_DeleteCollection_Confirmation_Title,
            new StandardDialogParameters()
            {
                Text = string.Format(Language.Loadout_DeleteCollection_Confirmation_Message, collectionName),
            },
            buttonDefinitions:
            [
                DialogStandardButtons.Cancel,
                new DialogButtonDefinition(Language.Loadout_DeleteCollection_Confirmation_Delete,
                    ButtonDefinitionId.Accept,
                    ButtonAction.Accept,
                    ButtonStyling.Destructive
                )
            ]
        );

        var result = await _windowManager.ShowDialog(dialog, DialogWindowType.Modal);
        return result.ButtonId == ButtonDefinitionId.Accept;
    }
}
