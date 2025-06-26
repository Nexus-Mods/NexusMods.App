using FluentAssertions;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.TreeDataGrid.Filters;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;

namespace NexusMods.UI.Tests.Controls.TreeDataGrid.Filters;

public class FilterTests
{
    [Fact]
    public void NoFilter_ShouldPassAllItems()
    {
        // Arrange
        var model = CreateTestModel();
        var filter = new Filter.NoFilter();

        // Act
        var result = model.MatchesFilter(filter);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void NameFilter_CaseSensitive_ShouldMatchExactCase()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("name"), new NameComponent("TestMod"));
        
        var filter = new Filter.NameFilter("TestMod", CaseSensitive: true);

        // Act
        var result = model.MatchesFilter(filter);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void NameFilter_CaseSensitive_ShouldNotMatchDifferentCase()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("name"), new NameComponent("TestMod"));
        
        var filter = new Filter.NameFilter("testmod", CaseSensitive: true);

        // Act
        var result = model.MatchesFilter(filter);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void NameFilter_CaseInsensitive_ShouldMatchDifferentCase()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("name"), new NameComponent("TestMod"));
        
        var filter = new Filter.NameFilter("testmod", CaseSensitive: false);

        // Act
        var result = model.MatchesFilter(filter);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void NameFilter_Substring_ShouldMatch()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("name"), new NameComponent("MyAwesomeTestMod"));
        
        var filter = new Filter.NameFilter("Test", CaseSensitive: false);

        // Act
        var result = model.MatchesFilter(filter);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void InstalledFilter_ShowInstalled_ShouldMatchInstalledItems()
    {
        // Arrange
        var model = CreateTestModel();
        var installAction = CreateInstallAction(isInstalled: true);
        model.Add(ComponentKey.From("install"), installAction);
        
        var filter = new Filter.InstalledFilter(ShowInstalled: true, ShowNotInstalled: false);

        // Act
        var result = model.MatchesFilter(filter);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void InstalledFilter_ShowNotInstalled_ShouldMatchNotInstalledItems()
    {
        // Arrange
        var model = CreateTestModel();
        var installAction = CreateInstallAction(isInstalled: false);
        model.Add(ComponentKey.From("install"), installAction);
        
        var filter = new Filter.InstalledFilter(ShowInstalled: false, ShowNotInstalled: true);

        // Act
        var result = model.MatchesFilter(filter);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void InstalledFilter_ShowInstalledOnly_ShouldNotMatchNotInstalledItems()
    {
        // Arrange
        var model = CreateTestModel();
        var installAction = CreateInstallAction(isInstalled: false);
        model.Add(ComponentKey.From("install"), installAction);
        
        var filter = new Filter.InstalledFilter(ShowInstalled: true, ShowNotInstalled: false);

        // Act
        var result = model.MatchesFilter(filter);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateAvailableFilter_ShouldMatchItemsWithUpdates()
    {
        // Arrange
        var model = CreateTestModel();
        var updateAction = CreateUpdateAction(); // Now using real UpdateAction
        model.Add(ComponentKey.From("update"), updateAction);
        
        var filter = new Filter.UpdateAvailableFilter(ShowWithUpdates: true, ShowWithoutUpdates: false);

        // Act
        var result = model.MatchesFilter(filter);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VersionFilter_ShouldMatchVersionPattern()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("version"), new StringComponent("1.2.3"));
        
        var filter = new Filter.VersionFilter("1.2");

        // Act
        var result = model.MatchesFilter(filter);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DateRangeFilter_ShouldMatchDatesInRange()
    {
        // Arrange
        var testDate = new DateTimeOffset(2023, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var model = CreateTestModel();
        model.Add(ComponentKey.From("date"), new DateComponent(testDate));
        
        var startDate = new DateTimeOffset(2023, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2023, 6, 30, 0, 0, 0, TimeSpan.Zero);
        var filter = new Filter.DateRangeFilter(startDate, endDate);

        // Act
        var result = model.MatchesFilter(filter);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DateRangeFilter_ShouldNotMatchDatesOutsideRange()
    {
        // Arrange
        var testDate = new DateTimeOffset(2023, 7, 15, 0, 0, 0, TimeSpan.Zero);
        var model = CreateTestModel();
        model.Add(ComponentKey.From("date"), new DateComponent(testDate));
        
        var startDate = new DateTimeOffset(2023, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2023, 6, 30, 0, 0, 0, TimeSpan.Zero);
        var filter = new Filter.DateRangeFilter(startDate, endDate);

        // Act
        var result = model.MatchesFilter(filter);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SizeRangeFilter_ShouldMatchSizesInRange()
    {
        // Arrange
        var testSize = Size.FromLong(50 * 1024 * 1024); // 50 MB
        var model = CreateTestModel();
        model.Add(ComponentKey.From("size"), new SizeComponent(testSize));
        
        var minSize = Size.FromLong(10 * 1024 * 1024); // 10 MB
        var maxSize = Size.FromLong(100 * 1024 * 1024); // 100 MB
        var filter = new Filter.SizeRangeFilter(minSize, maxSize);

        // Act
        var result = model.MatchesFilter(filter);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SizeRangeFilter_ShouldNotMatchSizesOutsideRange()
    {
        // Arrange
        var testSize = Size.FromLong(150 * 1024 * 1024); // 150 MB
        var model = CreateTestModel();
        model.Add(ComponentKey.From("size"), new SizeComponent(testSize));
        
        var minSize = Size.FromLong(10 * 1024 * 1024); // 10 MB
        var maxSize = Size.FromLong(100 * 1024 * 1024); // 100 MB
        var filter = new Filter.SizeRangeFilter(minSize, maxSize);

        // Act
        var result = model.MatchesFilter(filter);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AndFilter_ShouldRequireBothConditions()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("name"), new NameComponent("TestMod"));
        var installAction = CreateInstallAction(isInstalled: true);
        model.Add(ComponentKey.From("install"), installAction);
        
        var nameFilter = new Filter.NameFilter("Test", CaseSensitive: false);
        var installedFilter = new Filter.InstalledFilter(ShowInstalled: true, ShowNotInstalled: false);
        var andFilter = new Filter.AndFilter(nameFilter, installedFilter);

        // Act
        var result = model.MatchesFilter(andFilter);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AndFilter_ShouldFailIfOneConditionFails()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("name"), new NameComponent("TestMod"));
        var installAction = CreateInstallAction(isInstalled: false);
        model.Add(ComponentKey.From("install"), installAction);
        
        var nameFilter = new Filter.NameFilter("Test", CaseSensitive: false);
        var installedFilter = new Filter.InstalledFilter(ShowInstalled: true, ShowNotInstalled: false);
        var andFilter = new Filter.AndFilter(nameFilter, installedFilter);

        // Act
        var result = model.MatchesFilter(andFilter);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void OrFilter_ShouldPassIfEitherConditionPasses()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("name"), new NameComponent("TestMod"));
        var installAction = CreateInstallAction(isInstalled: false);
        model.Add(ComponentKey.From("install"), installAction);
        
        var nameFilter = new Filter.NameFilter("Test", CaseSensitive: false);
        var installedFilter = new Filter.InstalledFilter(ShowInstalled: true, ShowNotInstalled: false);
        var orFilter = new Filter.OrFilter(nameFilter, installedFilter);

        // Act
        var result = model.MatchesFilter(orFilter);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void NotFilter_ShouldInvertResult()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("name"), new NameComponent("TestMod"));
        
        var nameFilter = new Filter.NameFilter("Test", CaseSensitive: false);
        var notFilter = new Filter.NotFilter(nameFilter);

        // Act
        var result = model.MatchesFilter(notFilter);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Filter_WithIrrelevantComponents_ShouldPassThrough()
    {
        // Arrange
        var model = CreateTestModel();
        // Add a component that doesn't match the filter type
        model.Add(ComponentKey.From("changelog"), new LibraryComponents.ViewChangelogAction());
        
        var nameFilter = new Filter.NameFilter("Test", CaseSensitive: false);

        // Act
        var result = model.MatchesFilter(nameFilter);

        // Assert
        result.Should().BeTrue(); // Should pass through since no relevant components
    }

    [Fact]
    public void Filter_WithNoRelevantComponents_ShouldPassThrough()
    {
        // Arrange
        var model = CreateTestModel(); // Empty model
        var nameFilter = new Filter.NameFilter("Test", CaseSensitive: false);

        // Act
        var result = model.MatchesFilter(nameFilter);

        // Assert
        result.Should().BeTrue(); // Should pass through since no relevant components
    }

    private static CompositeItemModel<EntityId> CreateTestModel()
    {
        return new CompositeItemModel<EntityId>(EntityId.From(1));
    }

    private static LibraryComponents.InstallAction CreateInstallAction(bool isInstalled)
    {
        var valueComponent = new ValueComponent<bool>(
            initialValue: isInstalled, 
            valueObservable: R3.Observable.Return(isInstalled),
            subscribeWhenCreated: false
        );
        
        var libraryItemId = EntityId.From(1); // Use EntityId directly as LibraryItemId
        
        return new LibraryComponents.InstallAction(valueComponent, libraryItemId);
    }

    private static LibraryComponents.UpdateAction CreateUpdateAction()
    {
        // Create minimal mock structures for testing
        var mockFileMetadata = default(NexusModsFileMetadata.ReadOnly);
        var mockModUpdate = new ModUpdateOnPage(mockFileMetadata, [mockFileMetadata]);
        var mockModUpdates = new ModUpdatesOnModPage([mockModUpdate]);
        
        return new LibraryComponents.UpdateAction(
            initialValue: mockModUpdates,
            valuesObservable: R3.Observable.Return(mockModUpdates)
        );
    }

} 
