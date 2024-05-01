using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.MnemonicDB.Attributes;

namespace NexusMods.Games.TestFramework.Verifiers;

[UsedImplicitly]
internal record VerifiableMod
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required ModCategory Category { get; init; }

    public required List<VerifiableFile> Files { get; init; }

    public static VerifiableMod From(Mod.Model mod)
    {
        var files = mod.Files
            .Select(f => f.Remap<StoredFile.Model>())
            .Select(VerifiableFile.From)
            .OrderByDescending(file => file.To, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(file => file.Hash)
            .ToList();

        return new VerifiableMod
        {
            Name = mod.Name,
            Version = mod.Version,
            Category = mod.Category,
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

    public static VerifiableFile From(StoredFile.Model storedFile)
    {
        return new VerifiableFile
        {
            To = storedFile.To.ToString(),
            Size = storedFile.Size.Value,
            Hash = storedFile.Hash.Value,
        };
    }
}
