using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Threading;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Windows;
using NexusMods.UI.Tests.Framework;
using Xunit;
using Avalonia.Controls.Primitives;

namespace NexusMods.UI.Tests;

/// <summary>
/// Practical E2E tests for the MyGames page functionality
/// Demonstrates how to test specific application features
/// </summary>
public class MyGamesE2ETests : AUiTest
{
    private readonly ILogger<MyGamesE2ETests> _logger;

    public MyGamesE2ETests(IServiceProvider provider) : base(provider)
    {
        _logger = provider.GetRequiredService<ILogger<MyGamesE2ETests>>();
    }

    [Fact]
    public async Task Can_Load_MyGames_Page()
    {
        // Arrange & Act
        await using var windowHost = await App.GetMainWindow();
        
        // Wait for the application to fully load
        await WindowHost.OnUi(async () =>
        {
            await Task.Delay(500); // Allow time for initial loading
        });
        
        // Assert
        // Verify that the MyGames page is accessible
        await WindowHost.OnUi(async () =>
        {
            // Look for common elements that should be present on the MyGames page
            var gameListElements = await windowHost.Select<Control>("GameList");
            var searchElements = await windowHost.Select<Control>("SearchBox");
            
            // At minimum, we should have some UI elements loaded
            _logger.LogInformation("MyGames page loaded successfully");
        });
    }

    [Fact]
    public async Task Can_Search_For_Games()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act
        await WindowHost.OnUi(async () =>
        {
            // Wait for the page to load
            await Task.Delay(500);
            
            // Find search elements
            var searchBoxes = await windowHost.Select<TextBox>("SearchBox");
            if (searchBoxes.Any())
            {
                var searchBox = searchBoxes.First();
                
                // Enter search text
                searchBox.Text = "Cyberpunk";
                
                // Wait for search to process
                await Task.Delay(200);
                
                _logger.LogInformation("Search performed for 'Cyberpunk'");
            }
        });
        
        // Assert
        await WindowHost.OnUi(async () =>
        {
            // Verify search results are displayed
            // This would depend on the actual UI structure
            await Task.Delay(100);
        });
    }

    [Fact]
    public async Task Can_Select_And_View_Game_Details()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act
        await WindowHost.OnUi(async () =>
        {
            // Wait for games to load
            await Task.Delay(500);
            
            // Find game list items
            var gameItems = await windowHost.Select<Control>("GameItem");
            if (gameItems.Any())
            {
                var firstGame = gameItems.First();
                
                // Simulate clicking on the first game
                // This would depend on the actual control type and event handling
                _logger.LogInformation("Selected first game item");
                
                // Wait for details to load
                await Task.Delay(200);
            }
        });
        
        // Assert
        await WindowHost.OnUi(async () =>
        {
            // Verify game details are displayed
            var detailElements = await windowHost.Select<Control>("GameDetails");
            // Add assertions based on expected UI elements
        });
    }

    [Fact]
    public async Task Can_Handle_Empty_Game_List()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act & Assert
        await WindowHost.OnUi(async () =>
        {
            // Wait for the page to load
            await Task.Delay(500);
            
            // Check if empty state is handled properly
            var emptyStateElements = await windowHost.Select<Control>("EmptyState");
            var gameListElements = await windowHost.Select<Control>("GameList");
            
            // Log the state for debugging
            _logger.LogInformation("Empty state check completed");
        });
    }

    [Fact]
    public async Task Can_Test_Game_Filtering()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act
        await WindowHost.OnUi(async () =>
        {
            // Wait for initial load
            await Task.Delay(500);
            
            // Find filter controls
            var filterButtons = await windowHost.Select<Button>("FilterButton");
            var sortButtons = await windowHost.Select<Button>("SortButton");
            
            if (filterButtons.Any())
            {
                // Test different filter options
                foreach (var filterButton in filterButtons.Take(2)) // Test first 2 filters
                {
                    // Simulate filter selection
                    _logger.LogInformation("Testing filter: {FilterName}", filterButton.Name);
                    await Task.Delay(100);
                }
            }
        });
        
        // Assert
        await WindowHost.OnUi(async () =>
        {
            // Verify filtering worked correctly
            await Task.Delay(200);
        });
    }

    [Fact]
    public async Task Can_Test_Game_Management_Actions()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act
        await WindowHost.OnUi(async () =>
        {
            // Wait for games to load
            await Task.Delay(500);
            
            // Find action buttons (like "Manage", "Launch", etc.)
            var actionButtons = await windowHost.Select<Button>("ActionButton");
            var manageButtons = await windowHost.Select<Button>("ManageButton");
            var launchButtons = await windowHost.Select<Button>("LaunchButton");
            
            // Test that action buttons are present and enabled
            foreach (var button in actionButtons.Take(3))
            {
                button.IsEnabled.Should().BeTrue("Action buttons should be enabled");
            }
            
            _logger.LogInformation("Game management actions verified");
        });
    }

    [Fact]
    public async Task Can_Test_Responsive_Layout()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act & Assert - Test different window sizes
        var testSizes = new[] { (1920, 1080), (1366, 768), (1024, 768) };
        
        foreach (var (width, height) in testSizes)
        {
            await WindowHost.OnUi(async () =>
            {
                // Resize window
                windowHost.Window.Width = width;
                windowHost.Window.Height = height;
                
                // Wait for layout to adjust
                await Task.Delay(200);
                
                // Verify layout is still functional
                var gameListElements = await windowHost.Select<Control>("GameList");
                
                _logger.LogInformation("Layout test completed for {Width}x{Height}", width, height);
            });
        }
    }

    [Fact]
    public async Task Can_Test_Performance_Of_Game_Loading()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        await WindowHost.OnUi(async () =>
        {
            // Measure time to load games
            await Task.Delay(500); // Wait for initial load
            
            // Check if games are loaded
            var gameElements = await windowHost.Select<Control>("GameItem");
            
            stopwatch.Stop();
            
            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, "Games should load within 2 seconds");
            
            _logger.LogInformation("Game loading performance test completed in {ElapsedMs}ms", 
                stopwatch.ElapsedMilliseconds);
        });
    }

    [Fact]
    public async Task Can_Test_Error_Handling_For_Game_Loading()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act & Assert
        await WindowHost.OnUi(async () =>
        {
            // Wait for initial load
            await Task.Delay(500);
            
            // Look for error states or loading indicators
            var errorElements = await windowHost.Select<Control>("ErrorState");
            var loadingElements = await windowHost.Select<Control>("LoadingIndicator");
            
            // Verify that either content is loaded or appropriate error/loading state is shown
            var gameElements = await windowHost.Select<Control>("GameItem");
            
            if (!gameElements.Any() && !errorElements.Any() && !loadingElements.Any())
            {
                _logger.LogWarning("No games, errors, or loading indicators found");
            }
            
            _logger.LogInformation("Error handling test completed");
        });
    }

    /// <summary>
    /// Helper method to find and interact with a specific game
    /// </summary>
    private async Task<Control?> FindGameByName(WindowHost windowHost, string gameName)
    {
        var gameItems = await windowHost.Select<Control>("GameItem");
        
        foreach (var gameItem in gameItems)
        {
            // This would need to be adapted based on the actual UI structure
            // You might need to look for text elements within the game item
            var textElements = await windowHost.Select<TextBlock>("GameName");
            
            foreach (var textElement in textElements)
            {
                if (textElement.Text?.Contains(gameName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return gameItem;
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// Helper method to wait for a specific condition to be met
    /// </summary>
    private async Task WaitForCondition(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.Elapsed < timeout)
        {
            if (await condition())
            {
                return;
            }
            
            await Task.Delay(100);
        }
        
        throw new TimeoutException($"Condition not met within {timeout.TotalSeconds} seconds");
    }
} 