using Avalonia.Controls;
using FluentAssertions;
using NexusMods.App.UI.RightContent.LoadoutGrid;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using NexusMods.UI.Tests.Framework;

namespace NexusMods.UI.Tests;

public class LoadoutGridViewTests : AViewTest<LoadoutGridView, LoadoutGridDesignViewModel, ILoadoutGridViewModel>
{
    public LoadoutGridViewTests(IServiceProvider provider, AvaloniaApp app) : base(provider, app) { }

    [Fact]
    public async Task CanDeleteModsFromLoadout()
    {
        var control = await GetControl<DataGrid>("ModsDataGrid");
        
        var ids = new List<ModCursor>();
        await Host.OnUi(async () =>
        {
            control.Items.OfType<ModCursor>().Should().HaveCount(9);

            for (int i = 0; i < Random.Shared.Next(1, 4); i++)
            {
                ids.Add(control.Items.OfType<ModCursor>().ElementAt(i));
                control.SelectedItems.Add(control.Items.OfType<ModCursor>().ElementAt(i));
            }

        });

        var deleteButton = await GetControl<Button>("DeleteModsButton");
        
        await Host.Click(deleteButton);
        
        await Host.OnUi(async () =>
        {
            control.Items.OfType<ModCursor>().Should().HaveCount(9 - ids.Count);
            foreach (var item in ids)
            {
                control.Items.OfType<ModCursor>().Should().NotContain(item);
            }
        });
    }
}
