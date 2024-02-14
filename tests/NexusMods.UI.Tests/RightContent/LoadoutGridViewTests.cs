using System.Collections.Concurrent;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using FluentAssertions;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Pages.LoadoutGrid;

namespace NexusMods.UI.Tests.RightContent;

public class LoadoutGridViewTests : AViewTest<LoadoutGridView, LoadoutGridDesignViewModel, ILoadoutGridViewModel>
{
    public LoadoutGridViewTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    public async Task CanDeleteModsFromLoadout()
    {
        var control = await GetControl<DataGrid>("ModsDataGrid");

        var ids = new List<ModCursor>();
        await OnUi(() =>
        {
            control.ItemsSource.OfType<ModCursor>().Should().HaveCount(9);

            for (int i = 0; i < Random.Shared.Next(1, 4); i++)
            {
                ids.Add(control.ItemsSource.OfType<ModCursor>().ElementAt(i));
                control.SelectedItems.Add(control.ItemsSource.OfType<ModCursor>().ElementAt(i));
            }

        });

        var deleteButton = await GetControl<Button>("DeleteModsButton");

        await Click(deleteButton);

        await OnUi(() =>
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

        await OnUi(() =>
        {
            ViewModel.AddMod(new ModCursor(ViewModel.LoadoutId, ModId.NewId()));
        });


        await Eventually(async () =>
        {
            events.Should().HaveCount(1);
            (await GetVisualDescendants<DataGridRow>(rowsPresenter)).Should().HaveCount(10);
        });
    }
}
