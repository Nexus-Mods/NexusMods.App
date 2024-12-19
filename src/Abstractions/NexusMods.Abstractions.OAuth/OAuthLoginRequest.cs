using System.Diagnostics;

namespace NexusMods.Abstractions.OAuth;

public record OAuthLoginRequest
{
    private TaskCompletionSource<Uri?> _taskCompletionSource = new();
    
    /// <summary>
    /// The result of the OAuth login task
    /// </summary>
    public Task<Uri?> Task => _taskCompletionSource.Task;
    
    /// <summary>
    /// The callback type for the OAuth login
    /// </summary>
    public required CallbackType CallbackType { get; init; }
    
    /// <summary>
    /// The authorization URL for the OAuth login
    /// </summary>
    public required Uri AuthorizationUrl { get; init; }
    
    /// <summary>
    /// The expected callback prefix for the OAuth login
    /// </summary>
    public required Uri CallbackPrefix { get; init; }
    
    public void Callback(Uri responseUri)
    {
        _taskCompletionSource.TrySetResult(responseUri);
    }
}
