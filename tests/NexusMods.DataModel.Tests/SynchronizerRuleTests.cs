using System.Text;
using DynamicData.Kernel;
using NexusMods.DataModel.LoadoutSynchronizer.Rules;
using NexusMods.Hashing.xxHash64;
using Xunit.DependencyInjection;

namespace NexusMods.DataModel.Tests;

public class SynchronizerRuleTests
{

    [Theory]
    [MethodData(nameof(TestRows))]
    public void AllRulesHaveActions(Signature signature, string EnumShorthand, Optional<Hash> disk, Optional<Hash> prev, Optional<Hash> loadout)
    {
        Assert.True(false);
    }

    [Fact]
    public async Task RulesAreAsExpected()
    {
        var allRules = AllSignatures().ToArray();

        var sb = new StringBuilder();

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Summary of all {allRules.Length} possible signatures in a somewhat readable format");
        sb.AppendLine("/// </summary>");
        
        sb.AppendLine("public enum SignatureShorthand : ushort");
        sb.AppendLine("{");
        foreach (var row in allRules)
        {
            sb.AppendLine("\t/// <summary>");
            sb.AppendLine($"\t/// {row.Signature}");
            sb.AppendLine("\t/// </summary>");
            sb.AppendLine($"\t{row.EnumShorthand} = 0x{row.Signature.ToString("x")},");
        }
        sb.AppendLine("}");

        await Verify(sb.ToString());
    }


    public static readonly Hash Hash1 = Hash.From(0xFAD01);
    public static readonly Hash Hash2 = Hash.From(0xFAD02);
    public static readonly Hash Hash3 = Hash.From(0xFAD03);

    public static IEnumerable<object[]> TestRows()
    {
        return AllSignatures().Select(row => new object[]
            {
                row.Signature, row.EnumShorthand, row.Disk, row.Prev, row.Loadout,
            }
        );
    }
    
    public static IEnumerable<(Signature Signature, string EnumShorthand, Optional<Hash> Disk, Optional<Hash> Prev, Optional<Hash> Loadout)> AllSignatures()
    {

        var options =
            from disk in new[] { Optional.None<Hash>(), Hash1, Hash2, Hash3 }
            from prev in new[] { Optional.None<Hash>(), Hash1, Hash2, Hash3 }
            from loadout in new[] { Optional.None<Hash>(), Hash1, Hash2, Hash3 }
            from isIgnored in new[] { false, true }
            from archivedState in new Hash[][] { [], [Hash1], [Hash2], [Hash3], [Hash1, Hash2], [Hash1, Hash3], [Hash2, Hash3], [Hash1, Hash2, Hash3] }
            where disk.HasValue || prev.HasValue || loadout.HasValue
            let sig = new SignatureBuilder
            {
                DiskHash = disk,
                PrevHash = prev,
                LoadoutHash = loadout,
                DiskArchived = disk.HasValue && archivedState.Contains(disk.Value),
                PrevArchived = prev.HasValue && archivedState.Contains(prev.Value),
                LoadoutArchived = loadout.HasValue && archivedState.Contains(loadout.Value),
                PathIsIgnored = isIgnored,
            }.Build()
            let enumShorthand = MakeShorthand(sig, disk, prev, loadout)
            select (sig, enumShorthand, disk, prev, loadout);

        return options.DistinctBy(o => o.sig);
    }

    private static string MakeShorthand(Signature sig, Optional<Hash> disk, Optional<Hash> prev, Optional<Hash> loadout)
    {
        var diskCode = HashToValue(disk);
        var prevCode = HashToValue(prev);
        var loadoutCode = HashToValue(loadout);
        
        var archived = (disk.HasValue && sig.HasFlag(Signature.DiskArchived) ? "X" : "x") +
                       (prev.HasValue && sig.HasFlag(Signature.PrevArchived) ? "X" : "x") +
                       (loadout.HasValue && sig.HasFlag(Signature.LoadoutArchived) ? "X" : "x");
        
        var ignored = sig.HasFlag(Signature.PathIsIgnored) ? "I" : "i";
        
        return $"{diskCode}{prevCode}{loadoutCode}_{archived}_{ignored}";
        
    }

    private static string HashToValue(Optional<Hash> hash)
    {
        if (!hash.HasValue) return "x";

        if (hash.Value == Hash1) return "A";
        if (hash.Value == Hash2) return "B";
        if (hash.Value == Hash3) return "C";
        throw new ArgumentOutOfRangeException(nameof(hash));
    }
}
