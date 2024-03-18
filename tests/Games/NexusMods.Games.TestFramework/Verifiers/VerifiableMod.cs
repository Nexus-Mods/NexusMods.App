using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;

namespace NexusMods.Games.TestFramework.Verifiers;

[UsedImplicitly]
internal record VerifiableMod
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required string Category { get; init; }

    public required List<VerifiableFile> Files { get; init; }

    public static VerifiableMod From(Mod mod)
    {
        var files = mod.Files.Values
            .OfType<StoredFile>()
            .Select(VerifiableFile.From)
            .OrderByDescending(file => file.To, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(file => file.Hash)
            .ToList();

        return new VerifiableMod
        {
            Name = mod.Name,
            Version = mod.Version,
            Category = mod.ModCategory,
            Files = files,
        };
    }
}

[UsedImplicitly]
internal record VerifiableFile
{
    public required string To { get; init; }

    public required ulong Size { get; init; }

    public required ulong Hash { get; init; }

    public static VerifiableFile From(StoredFile storedFile)
    {
        return new VerifiableFile
        {
            To = storedFile.To.ToString(),
            Size = storedFile.Size.Value,
            Hash = storedFile.Hash.Value,
        };
    }
}
