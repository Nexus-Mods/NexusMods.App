using NexusMods.Networking.NexusWebApi.Auth;
using Xunit;

namespace NexusMods.Games.TestFramework;

/// <summary>
/// Helper methods for API key-dependent tests.
/// </summary>
public static class ApiKeyTestHelper
{
    /// <summary>
    /// Checks if the NEXUS_API_KEY environment variable is set.
    /// </summary>
    public static bool IsApiKeyAvailable() 
    {
        return !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable(ApiKeyMessageFactory.NexusApiKeyEnvironmentVariable)
        );
    }
    
    /// <summary>
    /// Skips the test if the NEXUS_API_KEY environment variable is not set.
    /// Use this in SkippableFact tests that require API access.
    /// </summary>
    public static void SkipIfApiKeyNotAvailable()
    {
        Skip.If(!IsApiKeyAvailable(), $"Test requires {ApiKeyMessageFactory.NexusApiKeyEnvironmentVariable} environment variable to be set");
    }
}