namespace NexusMods.Abstractions.Steam;

/// <summary>
/// Interface for abstracting away the storage of Steam authentication data.
/// </summary>
public interface IAuthStorage
{
    /// <summary>
    /// Tries to load the authentication data, if it does not exist or fails to load, returns false.
    /// </summary>
    public Task<(bool Success, byte[] Data)> TryLoad();
    
    /// <summary>
    /// Saves the authentication data.
    /// </summary>
    public Task SaveAsync(byte[] data);
}
