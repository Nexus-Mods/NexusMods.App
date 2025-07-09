using FluentAssertions;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Filters;
using NexusMods.App.UI.Controls.TreeDataGrid.Filters;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Resources;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using static NexusMods.App.UI.Controls.Filters.Filter;

namespace NexusMods.UI.Tests.Controls.TreeDataGrid.Filters;

public class FilterTests
{
    [Fact]
    public void NoFilter_ShouldPassAllItems()
    {
        // Arrange
        var model = CreateTestModel();
        var filter = NoFilter.Instance;

        // Act
        var result = filter.MatchesRow(model);

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
        var result = filter.MatchesRow(model);

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
        var result = filter.MatchesRow(model);

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
        var result = filter.MatchesRow(model);

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
        var result = filter.MatchesRow(model);

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
        var result = filter.MatchesRow(model);

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
        var result = filter.MatchesRow(model);

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
        var result = filter.MatchesRow(model);

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
        var result = filter.MatchesRow(model);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VersionFilter_ShouldMatchVersionPattern()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("version"), new VersionComponent("1.2.3"));
        
        var filter = new Filter.VersionFilter("1.2");

        // Act
        var result = filter.MatchesRow(model);

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
        var result = filter.MatchesRow(model);

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
        var result = filter.MatchesRow(model);

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
        var result = filter.MatchesRow(model);

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
        var result = filter.MatchesRow(model);

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
        var result = andFilter.MatchesRow(model);

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
        var result = andFilter.MatchesRow(model);

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
        var result = orFilter.MatchesRow(model);

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
        var result = notFilter.MatchesRow(model);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Filter_WithIrrelevantComponents_ShouldFail()
    {
        // Arrange
        var model = CreateTestModel();
        // Add a component that doesn't match the filter type
        model.Add(ComponentKey.From("changelog"), new LibraryComponents.ViewChangelogAction());
        
        var nameFilter = new Filter.NameFilter("Test", CaseSensitive: false);

        // Act
        var result = nameFilter.MatchesRow(model);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Filter_WithNoRelevantComponents_ShouldFail()
    {
        // Arrange
        var model = CreateTestModel(); // Empty model
        var nameFilter = new Filter.NameFilter("Test", CaseSensitive: false);

        // Act
        var result = nameFilter.MatchesRow(model);

        // Assert
        result.Should().BeFalse(); 
    }

    // For the first tests, the most important components.
    [Fact]
    public void TextFilter_ShouldMatchNameComponent()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("name"), new NameComponent("TestMod"));
        
        var filter = new Filter.TextFilter("Test", CaseSensitive: false);

        // Act
        var result = filter.MatchesRow(model);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TextFilter_ShouldMatchStringComponent()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("description"), new StringComponent("This is a test description"));
        
        var filter = new Filter.TextFilter("description", CaseSensitive: false);

        // Act
        var result = filter.MatchesRow(model);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TextFilter_ShouldMatchVersionComponent()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("version"), new VersionComponent("1.2.3-beta"));
        
        var filter = new Filter.TextFilter("beta", CaseSensitive: false);

        // Act
        var result = filter.MatchesRow(model);

        // Assert
        result.Should().BeTrue();
    }

    // Now test TextFilter specific logic
    [Fact]
    public void TextFilter_ShouldMatchAnyComponentWithText()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("name"), new NameComponent("SomeMod"));
        model.Add(ComponentKey.From("version"), new VersionComponent("1.0.0"));
        model.Add(ComponentKey.From("description"), new StringComponent("This mod has special features"));
        
        var filter = new Filter.TextFilter("special", CaseSensitive: false);

        // Act
        var result = filter.MatchesRow(model);

        // Assert
        result.Should().BeTrue(); // Should match the description component
    }

    [Fact]
    public void TextFilter_ShouldFailIfNoComponentMatches()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("name"), new NameComponent("SomeMod"));
        model.Add(ComponentKey.From("version"), new VersionComponent("1.0.0"));
        model.Add(ComponentKey.From("description"), new StringComponent("Basic description"));
        
        var filter = new Filter.TextFilter("nonexistent", CaseSensitive: false);

        // Act
        var result = filter.MatchesRow(model);

        // Assert
        result.Should().BeFalse(); // No component should match
    }

    [Fact]
    public void TextFilter_EmptyString_ShouldMatchAllItems()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("name"), new NameComponent("TestMod"));
        model.Add(ComponentKey.From("version"), new VersionComponent("1.0.0"));
        
        var filter = new Filter.TextFilter("", CaseSensitive: false);

        // Act
        var result = filter.MatchesRow(model);

        // Assert
        result.Should().BeTrue(); // Empty string should match all items
    }

    // Ensure the magic 'TextFilter' works with existing logical operators.
    [Fact]
    public void TextFilter_WithLogicalOperators_ShouldWork()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("name"), new NameComponent("TestMod"));
        var installAction = CreateInstallAction(isInstalled: true);
        model.Add(ComponentKey.From("install"), installAction);
        
        var textFilter = new Filter.TextFilter("Test", CaseSensitive: false);
        var installedFilter = new Filter.InstalledFilter(ShowInstalled: true, ShowNotInstalled: false);
        var andFilter = new Filter.AndFilter(textFilter, installedFilter);

        // Act
        var result = andFilter.MatchesRow(model);

        // Assert
        result.Should().BeTrue(); // Both text and installed filters should match
    }

    [Fact]
    public void TextFilter_ShouldMatchSizeComponent()
    {
        // Arrange
        var testSize = Size.FromLong(50 * 1024 * 1024); // 50 MiB
        var model = CreateTestModel();
        model.Add(ComponentKey.From("size"), new SizeComponent(testSize));
        
        var filter = new Filter.TextFilter("50", CaseSensitive: false);

        // Act
        var result = filter.MatchesRow(model);

        // Assert
        result.Should().BeTrue(); // Should match the formatted size "50 MiB"
    }



    // Tests for localized components.

    // Note(sewer): I skipped the 'date' component because the date is formatted in the user's
    // current UI culture. If the person working on this codebase does not use 'Arabic' 0-9 numbers 
    // (actually Indian btw), this test will fail; and I don't want to risk that.
    [Fact]
    public void TextFilter_ShouldMatchInstallActionButtonText()
    {
        // Arrange
        var model = CreateTestModel();
        var installAction = CreateInstallAction(isInstalled: false);
        model.Add(ComponentKey.From("install"), installAction);
        
        var filter = new Filter.TextFilter(Language.LibraryComponents_InstallAction_ButtonText_Install, CaseSensitive: false);

        // Act
        var result = filter.MatchesRow(model);

        // Assert
        result.Should().BeTrue(); // Should match the localized "Install" button text
    }

    [Fact]
    public void TextFilter_ShouldMatchInstalledActionButtonText()
    {
        // Arrange
        var model = CreateTestModel();
        var installAction = CreateInstallAction(isInstalled: true);
        model.Add(ComponentKey.From("install"), installAction);
        
        var filter = new Filter.TextFilter(Language.LibraryComponents_InstallAction_ButtonText_Installed, CaseSensitive: false);

        // Act
        var result = filter.MatchesRow(model);

        // Assert
        result.Should().BeTrue(); // Should match the localized "Installed" button text
    }


    [Fact]
    public void TextFilter_ShouldMatchPartialSizeText()
    {
        // Arrange
        var testSize = Size.FromLong(1024 * 1024 * 1024 + 512 * 1024 * 1024); // 1.5 GiB (1.61 GB)
        var model = CreateTestModel();
        model.Add(ComponentKey.From("size"), new SizeComponent(testSize));
        
        var filter = new Filter.TextFilter("GB", CaseSensitive: false);

        // Act
        var result = filter.MatchesRow(model);

        // Assert
        result.Should().BeTrue(); // Should match the "GB" part of the formatted size
    }

    // Tests for some unusual components; but which funny people may nonetheless want to filter by.
    [Fact]
    public void TextFilter_ShouldMatchNewVersionAvailable_CurrentVersion()
    {
        // Arrange
        var model = CreateTestModel();
        var currentVersion = new VersionComponent("1.0.0");
        var newVersionAvailable = new LibraryComponents.NewVersionAvailable(
            currentVersion, 
            "2.0.0", 
            R3.Observable.Return("2.0.0"));
        model.Add(ComponentKey.From("newVersion"), newVersionAvailable);
        
        var filter = new Filter.TextFilter("1.0.0", CaseSensitive: false);

        // Act
        var result = filter.MatchesRow(model);

        // Assert
        result.Should().BeTrue(); // Should match the current version "1.0.0"
    }

    [Fact]
    public void TextFilter_ShouldMatchNewVersionAvailable_NewVersion()
    {
        // Arrange
        var model = CreateTestModel();
        var currentVersion = new VersionComponent("1.0.0");
        var newVersionAvailable = new LibraryComponents.NewVersionAvailable(
            currentVersion, 
            "2.0.0-beta", 
            R3.Observable.Return("2.0.0-beta"));
        model.Add(ComponentKey.From("newVersion"), newVersionAvailable);
        
        var filter = new Filter.TextFilter("beta", CaseSensitive: false);

        // Act
        var result = filter.MatchesRow(model);

        // Assert
        result.Should().BeTrue(); // Should match the "beta" part of the new version "2.0.0-beta"
    }

    [Fact]
    public void TextFilter_WhitespaceString_ShouldMatchWhenTextContainsWhitespace()
    {
        // Arrange
        var model = CreateTestModel();
        model.Add(ComponentKey.From("name"), new NameComponent("Test   Mod")); // Contains multiple spaces
        
        var filter = new Filter.TextFilter("   ", CaseSensitive: false);

        // Act
        var result = filter.MatchesRow(model);

        // Assert
        result.Should().BeTrue(); // Whitespace should match when the text actually contains that whitespace
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
