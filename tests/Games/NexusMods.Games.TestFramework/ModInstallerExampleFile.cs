namespace NexusMods.Games.TestFramework;

public record ModInstallerExampleFile
{
    public required ulong Hash { get; init; }
    public required string Name { get; init; } = string.Empty;

    public required byte[] Data { get; init; } = Array.Empty<byte>();
};
