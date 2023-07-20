using FluentAssertions;
using NexusMods.App.UI.Overlays.Generic.MessageBox.OkCancel;

namespace NexusMods.UI.Tests.Overlays.Generic.MessageBox.OkCancel;

/// <summary>
/// Tests the Generic Ok/Cancel MessageBox
/// </summary>
public class MessageBoxOkCancelTests : AViewTest<MessageBoxOkCancelView, MessageBoxOkCancelViewModel, IMessageBoxOkCancelViewModel>
{
    public MessageBoxOkCancelTests(IServiceProvider provider) : base(provider) { }
    
    [Fact]
    public async Task OkButton_ShouldSetDialogResultToTrue_AndIsActiveToFalse()
    {
        await OnUi(() =>
        {
            ViewModel.IsActive.Should().BeTrue();
            
            Click_AlreadyOnUi(View.OkButton);

            ViewModel.DialogResult.Should().BeTrue();
            ViewModel.IsActive.Should().BeFalse();
        });
    }

    [Fact]
    public async Task CancelButton_ShouldSetDialogResultToFalse_AndIsActiveToFalse()
    {
        await OnUi(() =>
        {
            ViewModel.IsActive.Should().BeTrue();
            
            Click_AlreadyOnUi(View.CancelButton);

            ViewModel.DialogResult.Should().BeFalse();
            ViewModel.IsActive.Should().BeFalse();
        });
    }

    [Fact]
    public async Task CloseButton_ShouldSetDialogResultToFalse_AndIsActiveToFalse()
    {
        await OnUi(() =>
        {
            ViewModel.IsActive.Should().BeTrue();
            
            Click_AlreadyOnUi(View.CloseButton);

            ViewModel.DialogResult.Should().BeFalse();
            ViewModel.IsActive.Should().BeFalse();
        });
    }
    
}
