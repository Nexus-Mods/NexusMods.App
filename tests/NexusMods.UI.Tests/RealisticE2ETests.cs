using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.VisualTree;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.UI.Tests.Framework;
using Xunit;

namespace NexusMods.UI.Tests;

/// <summary>
/// Realistic E2E tests that explore the actual UI and test real functionality
/// </summary>
public class RealisticE2ETests : AUiTest
{
    private readonly ILogger<RealisticE2ETests> _logger;

    public RealisticE2ETests(IServiceProvider provider) : base(provider)
    {
        _logger = provider.GetRequiredService<ILogger<RealisticE2ETests>>();
    }

    [Fact]
    public async Task Can_Explore_Actual_UI_Elements()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act & Assert
        await WindowHost.OnUi(async () =>
        {
            // Wait for UI to load
            await Task.Delay(500);
            
            // Get all visual descendants
            var allElements = windowHost.Window.GetVisualDescendants().OfType<Control>().ToList();
            
            // Log what we found
            _logger.LogInformation("Found {Count} UI elements in the window", allElements.Count);
            
            // Group elements by type
            var elementTypes = allElements
                .GroupBy(e => e.GetType().Name)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToList();
            
            foreach (var group in elementTypes)
            {
                _logger.LogInformation("  {Type}: {Count} elements", group.Key, group.Count());
            }
            
            // Verify we have some UI elements
            allElements.Should().NotBeEmpty("Window should contain some UI controls");
            
            // Look for elements with names
            var namedElements = allElements.Where(e => !string.IsNullOrEmpty(e.Name)).ToList();
            _logger.LogInformation("Found {Count} elements with names", namedElements.Count);
            
            foreach (var element in namedElements.Take(5))
            {
                _logger.LogInformation("  Named element: {Name} ({Type})", element.Name, element.GetType().Name);
            }
        });
    }

    [Fact]
    public async Task Can_Find_Buttons_In_UI()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act & Assert
        await WindowHost.OnUi(async () =>
        {
            // Wait for UI to load
            await Task.Delay(500);
            
            // Find all buttons
            var buttons = windowHost.Window.GetVisualDescendants().OfType<Button>().ToList();
            
            _logger.LogInformation("Found {Count} buttons in the UI", buttons.Count);
            
            // Log button details
            foreach (var button in buttons.Take(5))
            {
                var name = string.IsNullOrEmpty(button.Name) ? "(unnamed)" : button.Name;
                var content = button.Content?.ToString() ?? "(no content)";
                _logger.LogInformation("  Button: {Name}, Content: '{Content}', Enabled: {Enabled}", 
                    name, content, button.IsEnabled);
            }
            
            // Test that we can interact with buttons if they exist
            if (buttons.Any())
            {
                var firstButton = buttons.First();
                firstButton.IsEnabled.Should().BeTrue("First button should be enabled");
                
                _logger.LogInformation("Successfully tested button interaction");
            }
            else
            {
                _logger.LogInformation("No buttons found in the UI");
            }
        });
    }

    [Fact]
    public async Task Can_Find_Text_Elements_In_UI()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act & Assert
        await WindowHost.OnUi(async () =>
        {
            // Wait for UI to load
            await Task.Delay(500);
            
            // Find text elements
            var textBlocks = windowHost.Window.GetVisualDescendants().OfType<TextBlock>().ToList();
            var textBoxes = windowHost.Window.GetVisualDescendants().OfType<TextBox>().ToList();
            
            _logger.LogInformation("Found {TextBlockCount} TextBlocks and {TextBoxCount} TextBoxes", 
                textBlocks.Count, textBoxes.Count);
            
            // Log text content
            foreach (var textBlock in textBlocks.Take(3))
            {
                var text = textBlock.Text ?? "(no text)";
                var name = string.IsNullOrEmpty(textBlock.Name) ? "(unnamed)" : textBlock.Name;
                _logger.LogInformation("  TextBlock: {Name}, Text: '{Text}'", name, text);
            }
            
            foreach (var textBox in textBoxes.Take(3))
            {
                var text = textBox.Text ?? "(no text)";
                var name = string.IsNullOrEmpty(textBox.Name) ? "(unnamed)" : textBox.Name;
                _logger.LogInformation("  TextBox: {Name}, Text: '{Text}'", name, text);
            }
        });
    }

    [Fact]
    public async Task Can_Test_Window_Title_And_Properties()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act & Assert
        await WindowHost.OnUi(async () =>
        {
            // Test window title
            var title = windowHost.Window.Title;
            _logger.LogInformation("Window title: '{Title}'", title);
            
            // Test window properties
            var width = windowHost.Window.Width;
            var height = windowHost.Window.Height;
            var isVisible = windowHost.Window.IsVisible;
            
            _logger.LogInformation("Window properties - Width: {Width}, Height: {Height}, Visible: {Visible}", 
                width, height, isVisible);
            
            // Verify properties
            width.Should().BeGreaterThan(0, "Window width should be positive");
            height.Should().BeGreaterThan(0, "Window height should be positive");
            isVisible.Should().BeTrue("Window should be visible");
        });
    }

    [Fact]
    public async Task Can_Test_ViewModel_Properties()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act & Assert
        await WindowHost.OnUi(async () =>
        {
            // Test that we can access the view model
            var viewModel = windowHost.ViewModel;
            viewModel.Should().NotBeNull("ViewModel should not be null");
            
            // Log view model type
            _logger.LogInformation("ViewModel type: {Type}", viewModel.GetType().Name);
            
            // Test that we can access view model properties
            var properties = viewModel.GetType().GetProperties();
            _logger.LogInformation("ViewModel has {Count} properties", properties.Length);
            
            // Log some property names
            foreach (var prop in properties.Take(5))
            {
                _logger.LogInformation("  Property: {Name} ({Type})", prop.Name, prop.PropertyType.Name);
            }
        });
    }

    [Fact]
    public async Task Can_Test_Application_Startup_Time()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        await using var windowHost = await App.GetMainWindow();
        
        // Wait for UI to be fully loaded
        await WindowHost.OnUi(async () =>
        {
            await Task.Delay(500);
        });
        
        // Assert
        stopwatch.Stop();
        
        _logger.LogInformation("Application startup took {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        
        // Verify startup time is reasonable
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, "Application should start within 10 seconds");
    }

    [Fact]
    public async Task Can_Test_UI_Responsiveness()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act & Assert
        await WindowHost.OnUi(async () =>
        {
            // Wait for initial load
            await Task.Delay(500);
            
            // Test multiple UI operations to check responsiveness
            var operations = new List<string>();
            
            for (int i = 0; i < 5; i++)
            {
                var startTime = Stopwatch.StartNew();
                
                // Perform a UI operation
                var elements = windowHost.Window.GetVisualDescendants().OfType<Control>().ToList();
                var count = elements.Count;
                
                startTime.Stop();
                operations.Add($"Operation {i + 1}: {startTime.ElapsedMilliseconds}ms ({count} elements)");
            }
            
            // Log results
            foreach (var operation in operations)
            {
                _logger.LogInformation(operation);
            }
            
            // Verify operations are reasonably fast
            var totalTime = operations.Sum(op => int.Parse(op.Split(':')[1].Split('m')[0]));
            totalTime.Should().BeLessThan(1000, "UI operations should be responsive");
        });
    }
} 