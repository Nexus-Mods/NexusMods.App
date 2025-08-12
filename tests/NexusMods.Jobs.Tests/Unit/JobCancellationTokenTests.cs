using FluentAssertions;
using NexusMods.Abstractions.Jobs;
using Xunit;

namespace NexusMods.Jobs.Tests.Unit;

public class JobCancellationTokenTests
{
    [Fact]
    public void Should_Initialize_In_Running_State()
    {
        // Arrange & Act
        var token = new JobCancellationToken();

        // Assert
        token.IsPaused.Should().BeFalse();
        token.Token.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void Should_Support_Basic_Cancellation()
    {
        // Arrange
        var token = new JobCancellationToken();

        // Act
        token.Cancel();

        // Assert
        token.Token.IsCancellationRequested.Should().BeTrue();
        token.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public void Should_Cancel_And_Set_Cancellation_Token()
    {
        // Arrange
        var token = new JobCancellationToken();

        // Act
        token.Cancel();

        // Assert
        token.Token.IsCancellationRequested.Should().BeTrue();
        token.IsPaused.Should().BeFalse(); // Cancelled jobs are not considered paused
    }

    [Fact]
    public void Should_Cancel_And_Recycle_Token_on_Pause()
    {
        // Arrange
        var token = new JobCancellationToken();

        // Act
        var originalToken = token.Token;
        token.Pause();

        // Assert
        token.IsPaused.Should().BeTrue();
        
        // Original token should request cancellation,
        // while new token should be recycled
        originalToken.IsCancellationRequested.Should().BeTrue();
        token.Token.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void Should_Resume_From_Paused_State()
    {
        // Arrange
        var token = new JobCancellationToken();
        token.Pause();

        // Act
        token.Resume();

        // Assert
        token.IsPaused.Should().BeFalse();
    }

    [Fact]
    public void Should_Ignore_Resume_When_Not_Paused()
    {
        // Arrange
        var token = new JobCancellationToken();

        // Act
        token.Resume();

        // Assert
        token.IsPaused.Should().BeFalse();
    }

    [Fact]
    public void Should_Ignore_Resume_When_Cancelled()
    {
        // Arrange
        var token = new JobCancellationToken();
        token.Cancel();

        // Act
        token.Resume();

        // Assert
        token.Token.IsCancellationRequested.Should().BeTrue();
        token.IsPaused.Should().BeFalse();
    }
}
