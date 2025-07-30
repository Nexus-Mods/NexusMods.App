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
    public void Should_Link_To_External_CancellationToken()
    {
        // Arrange
        using var externalTokenSource = new CancellationTokenSource();
        var token = new JobCancellationToken(externalTokenSource.Token);

        // Act
        externalTokenSource.Cancel();

        // Assert
        token.Token.IsCancellationRequested.Should().BeTrue();
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
    public void Should_Pause_And_Set_Paused_State()
    {
        // Arrange
        var token = new JobCancellationToken();

        // Act
        token.Pause();

        // Assert
        token.IsPaused.Should().BeTrue();
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

    [Fact]
    public async Task Should_Block_On_WaitForResumeAsync_When_Paused()
    {
        // Arrange
        var token = new JobCancellationToken();
        var resumed = false;
        using var waitingStarted = new ManualResetEventSlim();

        // Act
        token.Pause();
        var waitTask = Task.Run(async () =>
        {
            // ReSharper disable once AccessToDisposedClosure
            waitingStarted.Set(); // Signal that we're about to start waiting
            await token.WaitForResumeAsync();
            resumed = true;
        });

        // Wait for the task to actually start waiting
        waitingStarted.Wait(TimeSpan.FromSeconds(30));
        resumed.Should().BeFalse();

        // Resume and verify
        token.Resume();
        await waitTask.WaitAsync(TimeSpan.FromSeconds(30));
        resumed.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Complete_WaitForResumeAsync_When_Not_Paused()
    {
        // Arrange
        var token = new JobCancellationToken();

        // Act & Assert
        var waitTask = token.WaitForResumeAsync();
        await waitTask.WaitAsync(TimeSpan.FromSeconds(100));
        waitTask.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled_During_Wait()
    {
        // Arrange
        var token = new JobCancellationToken();
        using var waitingStarted = new ManualResetEventSlim();
        token.Pause();

        // Act
        var waitTask = Task.Run(async () =>
        {
            // ReSharper disable once AccessToDisposedClosure
            waitingStarted.Set(); // Signal that we're about to start waiting
            await token.WaitForResumeAsync();
        });
        
        // Wait for the task to actually start waiting
        waitingStarted.Wait(TimeSpan.FromSeconds(30));
        token.Cancel();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitTask);
    }
}
