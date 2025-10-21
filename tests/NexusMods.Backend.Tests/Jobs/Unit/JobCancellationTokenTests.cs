using NexusMods.Sdk.Jobs;

namespace NexusMods.Backend.Tests.Jobs.Unit;

public class JobCancellationTokenTests
{
    [Test]
    public async Task Should_Initialize_In_Running_State()
    {
        // Arrange & Act
        var token = new JobCancellationToken();

        // Assert
        await Assert.That(token.IsPaused).IsFalse();
        await Assert.That(token.Token.IsCancellationRequested).IsFalse();
    }

    [Test]
    public async Task Should_Support_Basic_Cancellation()
    {
        // Arrange
        var token = new JobCancellationToken();

        // Act
        token.Cancel();

        // Assert
        await Assert.That(token.Token.IsCancellationRequested).IsTrue();
        await Assert.That(token.IsCancelled).IsTrue();
    }

    [Test]
    public async Task Should_Cancel_And_Set_Cancellation_Token()
    {
        // Arrange
        var token = new JobCancellationToken();

        // Act
        token.Cancel();

        // Assert
        await Assert.That(token.Token.IsCancellationRequested).IsTrue();
        await Assert.That(token.IsPaused).IsFalse(); // Cancelled jobs are not considered paused
    }

    [Test]
    public async Task Should_Cancel_And_Recycle_Token_on_Pause()
    {
        // Arrange
        var token = new JobCancellationToken();

        // Act
        var originalToken = token.Token;
        token.Pause();

        // Assert
        await Assert.That(token.IsPaused).IsTrue();
        
        // Original token should request cancellation,
        // while new token should be recycled
        await Assert.That(originalToken.IsCancellationRequested).IsTrue();
        await Assert.That(token.Token.IsCancellationRequested).IsFalse();
    }

    [Test]
    public async Task Should_Resume_From_Paused_State()
    {
        // Arrange
        var token = new JobCancellationToken();
        token.Pause();

        // Act
        token.Resume();

        // Assert
        await Assert.That(token.IsPaused).IsFalse();
    }

    [Test]
    public async Task Should_Ignore_Resume_When_Not_Paused()
    {
        // Arrange
        var token = new JobCancellationToken();

        // Act
        token.Resume();

        // Assert
        await Assert.That(token.IsPaused).IsFalse();
    }

    [Test]
    public async Task Should_Ignore_Resume_When_Cancelled()
    {
        // Arrange
        var token = new JobCancellationToken();
        token.Cancel();

        // Act
        token.Resume();

        // Assert
        await Assert.That(token.Token.IsCancellationRequested).IsTrue();
        await Assert.That(token.IsPaused).IsFalse();
    }
}
