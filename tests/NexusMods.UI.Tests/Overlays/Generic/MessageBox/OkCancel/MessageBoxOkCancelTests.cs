using FluentAssertions;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.Generic.MessageBox.OkCancel;

namespace NexusMods.UI.Tests.Overlays.Generic.MessageBox.OkCancel;

/// <summary>
/// Tests the Generic Ok/Cancel MessageBox
/// </summary>
public class MessageBoxOkCancelTests : AViewTest<MessageBoxOkCancelView, MessageBoxOkCancelViewModel, IMessageBoxOkCancelViewModel>
{
    public MessageBoxOkCancelTests(IServiceProvider provider) : base(provider) { }
    
    [Fact]
    public async Task OkButton_ShouldSetDialogResultToTrue_AndStatusToClosed()
    {
        var controler = new OverlayController();
        
        await OnUi(() =>
        {
            controler.Enqueue(ViewModel);
            
            Click_AlreadyOnUi(View.OkButton);

            ViewModel.Result.Should().BeTrue();
            ViewModel.Status.Should().Be(Status.Closed);
        });
    }

    [Fact]
    public async Task CancelButton_ShouldSetDialogResultToFalse_AndStatusIsClosed()
    {
        var controler = new OverlayController();
        
        await OnUi(() =>
        {
            controler.Enqueue((IOverlayViewModel)ViewModel);
            
            Click_AlreadyOnUi(View.CancelButton);

            ViewModel.Result.Should().BeFalse();
            ViewModel.Status.Should().Be(Status.Closed);
        });
    }

    [Fact]
    public async Task CloseButton_ShouldSetDialogResultToFalse_AndStatusIsClosed()
    {
        var controler = new OverlayController();
        
        await OnUi(() =>
        {
            controler.Enqueue((IOverlayViewModel)ViewModel);
            
            Click_AlreadyOnUi(View.CloseButton);

            ViewModel.Result.Should().BeFalse();
            ViewModel.Status.Should().Be(Status.Closed);
        });
    }
    
}
