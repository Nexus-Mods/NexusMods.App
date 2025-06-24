using System.Text;
using FluentAssertions;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;
using NexusMods.Sdk.FileStore;
using NexusMods.Sdk.IO;
using Xunit.Abstractions;

namespace NexusMods.Games.RedEngine.Tests;

public class Cyberpunk2077DiagnosticTests(ITestOutputHelper outputHelper) : ACyberpunkIsolatedGameTest<Cyberpunk2077DiagnosticTests>(outputHelper)
{
    public static string TemplateData(string diagnosticType) =>
        diagnosticType switch
        {
            "Virtual Atelier" => "\n\nprotected cb func RegisterMyStore(event: ref<VirtualShopRegistration>) -> Bool {\n\n",
            "Codeware" => "extends ScriptableService {\n\n",
            _ => "NO DATA",
        };

    /// <summary>
    /// Wrap the types in a theory data source.
    /// </summary>
    public static IEnumerable<object[]> PathBasedDiagnosticArgs 
        => PatternDefinitions.Definitions.Select(t => new object[] { t.DependencyName });

    [Fact]
    public void CyberpunkGameExposesPatternBasedDiagnostics()
    {
        Game.DiagnosticEmitters.Should().ContainSingle(e => e.GetType() == typeof(PatternBasedDependencyEmitter));
    }

    [Theory]
    [MemberData(nameof(PathBasedDiagnosticArgs))]
    public async Task PathBasedDiagnosticEmittersLookAtTheCorrectPaths(string diagnosticName)
    {
        PatternDefinitions.Definitions.Should()
            .ContainSingle(p => p.DependencyName == diagnosticName, "The pattern definitions should contain the diagnostic name");
        
        var pattern = PatternDefinitions.Definitions.First(t => t.DependencyName == diagnosticName);
        
        var emitter = Game.DiagnosticEmitters.OfType<PatternBasedDependencyEmitter>().FirstOrDefault();
        emitter.Should().NotBeNull();

        var loadout = await CreateLoadout();
        
        // Install the dependant but not the dependency
        {
            var filesToBackup = new List<ArchivedFileEntry>();
            using var tx = Connection.BeginTransaction();
            var pluginMod = AddEmptyGroup(tx, loadout, "PluginMod");
            foreach (var searchPattern in pattern.DependantSearchPatterns)
            {
                var relativePath = searchPattern.Path.Path.Join("testFile").WithExtension(searchPattern.Extension);
                var gamePath = new GamePath(searchPattern.Path.LocationId, relativePath);
                var content = TemplateData(pattern.DependencyName);
                var contentArray = Encoding.UTF8.GetBytes(content);
                AddFile(tx, loadout, pluginMod, gamePath, content);
                filesToBackup.Add(new ArchivedFileEntry(new MemoryStreamFactory(gamePath.FileName, new MemoryStream(contentArray)),
                    contentArray.xxHash3(),
                    Size.FromLong(contentArray.Length)));
                    
                AddFile(tx, loadout, pluginMod, gamePath);
            }

            await FileStore.BackupFiles(filesToBackup);
            await tx.Commit();
        }
        
        Refresh(ref loadout);

        (await HasMatchingDiagnostics(loadout)).Should().BeTrue();
        
        // Install the dependency and the diagnostic should disappear
        {
            using var tx = Connection.BeginTransaction();
            var dependencyMod = AddEmptyGroup(tx, loadout, "DependencyMod");
            foreach (var dependencyPath in pattern.DependencyPaths)
            {
                var gamePath = new GamePath(dependencyPath.LocationId, dependencyPath.Path);
                AddFile(tx, loadout, dependencyMod, gamePath);
            }
            await tx.Commit();
        }
        
        Refresh(ref loadout);

        (await HasMatchingDiagnostics(loadout)).Should().BeFalse();

        // Disable the dependency and the diagnostic should reappear
        {
            var dependencyMod = LoadoutItemGroup.Load(Connection.Db, loadout.Items.First(m => m.Name == "DependencyMod").Id);
            using var tx = Connection.BeginTransaction();
            tx.Add(dependencyMod, LoadoutItem.Disabled, Null.Instance);
            await tx.Commit();
        }
        
        Refresh(ref loadout);
        
        (await HasMatchingDiagnostics(loadout)).Should().BeTrue();

        return;

        async Task<bool> HasMatchingDiagnostics(Loadout.ReadOnly loadout)
        {
            var found = await emitter
                .Diagnose(loadout, CancellationToken.None)
                .ToListAsync();

            foreach (var itm in found)
            {
                string patternName = itm switch
                {
                    Diagnostic<Diagnostics.MissingModWithKnownNexusUriWithStringSegmentMessageData> urlWithSegment => urlWithSegment.MessageData.DependencyName,
                    Diagnostic<Diagnostics.DisabledGroupDependencyMessageData> disabledDependency => disabledDependency.MessageData.DependencyName,
                    Diagnostic<Diagnostics.MissingModWithKnownNexusUriMessageData> url => url.MessageData.DependencyName,
                    Diagnostic<Diagnostics.DisabledGroupDependencyWithStringSegmentMessageData> disabled => disabled.MessageData.DependencyName,
                    _ => string.Empty,
                };
                    
                if (patternName == pattern.DependencyName)
                    return true;
            }

            return false;
        }
    }
}
