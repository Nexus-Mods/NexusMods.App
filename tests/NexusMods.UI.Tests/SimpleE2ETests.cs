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
/// Simple E2E tests that focus on basic functionality
/// These tests are designed to be more reliable and less dependent on specific UI elements
/// </summary>
public class SimpleE2ETests : AUiTest
{
    private readonly ILogger<SimpleE2ETests> _logger;

    public SimpleE2ETests(IServiceProvider provider) : base(provider)
    {
        _logger = provider.GetRequiredService<ILogger<SimpleE2ETests>>();
    }

    [Fact]
    public async Task Can_Start_Application()
    {
        // Arrange & Act
        await using var windowHost = await App.GetMainWindow();
        
        // Assert
        windowHost.Should().NotBeNull();
        windowHost.Window.Should().NotBeNull();
        windowHost.ViewModel.Should().NotBeNull();
        
        _logger.LogInformation("Application started successfully");
    }

    [Fact]
    public async Task Can_Access_Window_Properties()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act & Assert
        await WindowHost.OnUi(async () =>
        {
            // Test basic window properties
            windowHost.Window.Width.Should().BeGreaterThan(0);
            windowHost.Window.Height.Should().BeGreaterThan(0);
            windowHost.Window.IsVisible.Should().BeTrue();
            
            _logger.LogInformation("Window properties verified - Width: {Width}, Height: {Height}", 
                windowHost.Window.Width, windowHost.Window.Height);
        });
    }

    [Fact]
    public async Task Can_Find_Basic_UI_Elements()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act & Assert
        await WindowHost.OnUi(async () =>
        {
            // Wait for UI to load
            await Task.Delay(500);
            
            // Look for any controls in the window (don't rely on specific names)
            var allControls = windowHost.Window.GetVisualDescendants().OfType<Control>().ToList();
            
            allControls.Should().NotBeEmpty("Window should contain some UI controls");
            
            _logger.LogInformation("Found {Count} UI controls in the window", allControls.Count);
            
            // Log some of the control types for debugging
            var controlTypes = allControls.Take(5).Select(c => c.GetType().Name).Distinct();
            _logger.LogInformation("Control types found: {Types}", string.Join(", ", controlTypes));
        });
    }

    [Fact]
    public async Task Can_Test_Window_Resize()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act & Assert
        await WindowHost.OnUi(async () =>
        {
            var originalWidth = windowHost.Window.Width;
            var originalHeight = windowHost.Window.Height;
            
            // Resize window
            windowHost.Window.Width = 1024;
            windowHost.Window.Height = 768;
            
            // Wait for resize to take effect
            await Task.Delay(100);
            
            // Verify resize worked
            windowHost.Window.Width.Should().Be(1024);
            windowHost.Window.Height.Should().Be(768);
            
            // Restore original size
            windowHost.Window.Width = originalWidth;
            windowHost.Window.Height = originalHeight;
            
            _logger.LogInformation("Window resize test completed successfully");
        });
    }

    [Fact]
    public async Task Can_Test_Application_Stability()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Perform some basic operations to test stability
        await WindowHost.OnUi(async () =>
        {
            // Wait for initial load
            await Task.Delay(500);
            
            // Get window properties multiple times
            for (int i = 0; i < 5; i++)
            {
                var width = windowHost.Window.Width;
                var height = windowHost.Window.Height;
                var isVisible = windowHost.Window.IsVisible;
                
                // Verify properties are valid
                width.Should().BeGreaterThan(0);
                height.Should().BeGreaterThan(0);
                isVisible.Should().BeTrue();
                
                await Task.Delay(50);
            }
        });
        
        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "Stability test should complete within 5 seconds");
        
        _logger.LogInformation("Application stability test completed in {ElapsedMs}ms", 
            stopwatch.ElapsedMilliseconds);
    }

    [Fact]
    public async Task Can_Test_Service_Provider_Access()
    {
        // Arrange & Act
        await using var windowHost = await App.GetMainWindow();
        
        // Assert - Verify we can access services
        Provider.Should().NotBeNull();
        
        // Test accessing some common services
        var logger = Provider.GetService<ILogger<SimpleE2ETests>>();
        logger.Should().NotBeNull();
        
        _logger.LogInformation("Service provider access test completed");
    }

    [Fact]
    public async Task Can_Test_UI_Thread_Operations()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act & Assert
        await WindowHost.OnUi(async () =>
        {
            // Test that we can execute code on the UI thread
            var result = await Task.FromResult("UI thread test");
            result.Should().Be("UI thread test");
            
            // Test that we can access window properties on UI thread
            var windowTitle = windowHost.Window.Title;
            windowTitle.Should().NotBeNull();
            
            _logger.LogInformation("UI thread operations test completed");
        });
    }

    [Fact]
    public async Task Can_Test_Eventually_Pattern()
    {
        // Arrange
        await using var windowHost = await App.GetMainWindow();
        
        // Act & Assert
        await WindowHost.OnUi(async () =>
        {
            // Test the Eventually pattern from AUiTest
            await Eventually(() =>
            {
                // This should always succeed since we're just checking window properties
                windowHost.Window.Width.Should().BeGreaterThan(0);
            }, TimeSpan.FromSeconds(2));
            
            _logger.LogInformation("Eventually pattern test completed");
        });
    }
} 