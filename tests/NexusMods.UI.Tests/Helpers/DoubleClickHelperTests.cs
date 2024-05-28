using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FluentAssertions;
using NexusMods.App.UI.Helpers;

namespace NexusMods.UI.Tests.Helpers;

public class DoubleClickHelperTests : AUiTest
{
    public DoubleClickHelperTests(IServiceProvider provider) : base(provider) { }

    // NOTE(erri120): Unable to unit test the PointerPressed variant because everything is marked internal...

    [Fact]
    public async Task Test_DoubleClick_Generic()
    {
        await OnUi(() =>
        {
            var control = new Button();
            var automationPeer = new ButtonAutomationPeer(control);

            var numPressed = 0;
            var numTriggered = 0;
            using var disposable1 = control.AddDisposableHandler(Button.ClickEvent, (_, _) =>
            {
                numPressed++;
            }, routes: RoutingStrategies.Bubble, handledEventsToo: true);
            using var disposable2 = control.AddDoubleClickHandler(Button.ClickEvent).Subscribe(_ => numTriggered += 1);

            numPressed.Should().Be(0);
            numTriggered.Should().Be(0);

            automationPeer.Invoke();

            numPressed.Should().Be(1);
            numTriggered.Should().Be(0, because: "button was clicked once, it wasn't a double-click");

            Sleep();

            automationPeer.Invoke();

            numPressed.Should().Be(2);
            numTriggered.Should().Be(0, because: "button was clicked twice, but the time difference was too great");

            automationPeer.Invoke();

            numPressed.Should().Be(3);
            numTriggered.Should().Be(1, because: "button was double-clicked");
        });

        return;
        static void Sleep() => Thread.Sleep(DoubleClickHelper.DefaultTimeout + TimeSpan.FromMilliseconds(10));
    }
}
