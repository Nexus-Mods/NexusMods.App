using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Collections;
using NexusMods.UI.Sdk.Icons;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public class NewCollectionViewModel : LeftMenuItemViewModel
{
    public NewCollectionViewModel(
        IServiceProvider serviceProvider,
        LoadoutId loadoutId,
        IWorkspaceController workspaceController,
        WorkspaceId workspaceId) : base(workspaceController, workspaceId, null!)
    {
        Text = new StringComponent(value: "New Collection");
        Icon = IconValues.Add;

        var connection = serviceProvider.GetRequiredService<IConnection>();

        NavigateCommand = ReactiveCommand.CreateFromTask<NavigationInformation>(async (navigationInfo, _) =>
        {
            var dialog = LoadoutDialogs.CreateCollection();
            var windowManager = serviceProvider.GetRequiredService<IWindowManager>();
            var result = await windowManager.ShowDialog(dialog, DialogWindowType.Modal);
            if (result.ButtonId != ButtonDefinitionId.Accept) return;

            var collectionGroup = await CollectionCreator.CreateNewCollectionGroup(connection, loadoutId, newName: result.InputText);

            var pageData = new PageData
            {
                FactoryId = LoadoutPageFactory.StaticId,
                Context = new LoadoutPageContext
                {
                    LoadoutId = loadoutId,
                    GroupScope = collectionGroup.CollectionGroupId,
                },
            };

            var behavior = workspaceController.GetOpenPageBehavior(pageData, navigationInfo);
            workspaceController.OpenPage(workspaceId, pageData, behavior);
        });
    }
}
