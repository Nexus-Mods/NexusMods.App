using System.Collections.Concurrent;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using FluentAssertions;
using NexusMods.App.UI.RightContent.LoadoutGrid;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using NexusMods.UI.Tests.Framework;

namespace NexusMods.UI.Tests;

public class LoadoutGridViewTests : AViewTest<LoadoutGridView, LoadoutGridDesignViewModel, ILoadoutGridViewModel>
{
    public LoadoutGridViewTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    public async Task CanDeleteModsFromLoadout()
    {
        var control = await GetControl<DataGrid>("ModsDataGrid");
        
        var ids = new List<ModCursor>();
        await Host.OnUi(async () =>
        {
            control.ItemsSource.OfType<ModCursor>().Should().HaveCount(9);

            for (int i = 0; i < Random.Shared.Next(1, 4); i++)
            {
                ids.Add(control.ItemsSource.OfType<ModCursor>().ElementAt(i));
                control.SelectedItems.Add(control.ItemsSource.OfType<ModCursor>().ElementAt(i));
            }

        });

        var deleteButton = await GetControl<Button>("DeleteModsButton");
        
        await Host.Click(deleteButton);
        
        await Host.OnUi(async () =>
        {
            control.ItemsSource.OfType<ModCursor>().Should().HaveCount(9 - ids.Count);
            foreach (var item in ids)
            {
                control.ItemsSource.OfType<ModCursor>().Should().NotContain(item);
            }
        });
    }

    [Fact]
    public async Task AddingModsUpdatesTheDatagrid()
    {
        var control = await GetControl<DataGrid>("ModsDataGrid");
        
        ConcurrentBag<NotifyCollectionChangedEventArgs> events = new();
        ((INotifyCollectionChanged) ViewModel.Mods).CollectionChanged += (sender, args) => events.Add(args);
        var rowsPresenter = (await GetVisualDescendants<DataGridRowsPresenter>(control)).First();

        await Eventually(async () =>
        {
            (await GetVisualDescendants<DataGridRow>(rowsPresenter)).Should().HaveCount(9);
        });
        
        events.Clear();

        await Host.OnUi(async () =>
        {
            ViewModel.AddMod(new ModCursor(ViewModel.LoadoutId, ModId.New()));
        });
        
        
        await Eventually(async () =>
        {
            events.Should().HaveCount(1);
            (await GetVisualDescendants<DataGridRow>(rowsPresenter)).Should().HaveCount(10);
        });
    }
}
