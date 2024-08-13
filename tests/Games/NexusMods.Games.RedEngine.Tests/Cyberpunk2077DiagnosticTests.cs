using System.Reactive.Linq;
using System.Text;
using FluentAssertions;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Tests;

public class Cyberpunk2077DiagnosticTests(IServiceProvider serviceProvider) : AGameTest<Cyberpunk2077.Cyberpunk2077Game>(serviceProvider)
{
    public static readonly string Source = "NexusMods.Games.RedEngine.Cyberpunk2077";
    
    /// <summary>
    /// The types of all the known path based diagnostics for Cyberpunk 2077, this is used to
    /// drive the tests for the diagnostics.
    /// </summary>
    public static Type[] PathBasedDiagnosticTypes =>
    [
        typeof(ArchiveXLMissingEmitter),
        typeof(CyberEngineTweaksMissingEmitter),
        typeof(Red4ExtMissingEmitter),
        typeof(TweakXLMissingEmitter),
        typeof(VirtualAtelierDependencyMatcher),
        typeof(AppearanceModMenuDependencyEmitter)
    ];

    public static string TemplateData(Type diagnosticType) =>
        diagnosticType.Name switch
        {
            nameof(VirtualAtelierDependencyMatcher) => "\n\nprotected cb func RegisterMyStore(event: ref<VirtualShopRegistration>) -> Bool {\n\n",
            _ => "NO DATA",
        };

    /// <summary>
    /// Wrap the types in a theory data source.
    /// </summary>
    public static IEnumerable<object[]> PathBasedDiagnosticArgs 
        => PathBasedDiagnosticTypes.Select(t => new object[] { t });

    [Theory]
    [MemberData(nameof(PathBasedDiagnosticArgs))]
    public void CyberpunkGameExposesAllPathBasedDiagnostics(Type diagnosticType)
    {
        Game.DiagnosticEmitters.Should().ContainSingle(e => e.GetType() == diagnosticType);
    }

    [Fact]
    public void AllPathBasedDiagnosticEmittersAreRegistered()
    {
        foreach (var emitter in Game.DiagnosticEmitters.OfType<APathBasedDependencyEmitter>())
        {
            PathBasedDiagnosticTypes.Should().ContainSingle(t => t == emitter.GetType());
        }
    }
    
    [Theory]
    [MemberData(nameof(PathBasedDiagnosticArgs))]
    public async Task PathBasedDiagnosticEmittersLookAtTheCorrectPaths(Type diagnosticType)
    {
        var emitter = Game.DiagnosticEmitters.First(t => t.GetType() == diagnosticType) as APathBasedDependencyEmitterWithNexusDownload;
        
        emitter.Should().NotBeNull();
        
        emitter!.DependencyPaths.Should().NotBeEmpty();
        emitter.DependantPaths.Should().NotBeEmpty();
        emitter.DependantExtensions.Should().NotBeEmpty();
        
        var loadout = await CreateLoadout();
        
        // Install the dependant but not the dependency
        {
            var filesToBackup = new List<ArchivedFileEntry>();
            using var tx = Connection.BeginTransaction();
            var pluginMod = AddEmptyGroup(tx, loadout, "PluginMod");
            foreach (var dependantPath in emitter.DependantPaths)
            {
                foreach (var dependantExtension in emitter.DependantExtensions)
                {
                    var relativePath = dependantPath.Path.Join("testFile").WithExtension(dependantExtension);
                    var gamePath = new GamePath(dependantPath.LocationId, relativePath);
                    var content = TemplateData(diagnosticType);
                    var contentArray = Encoding.UTF8.GetBytes(content);
                    AddFile(tx, loadout, pluginMod, gamePath, content);
                    filesToBackup.Add(new ArchivedFileEntry(new MemoryStreamFactory(gamePath.FileName, new MemoryStream(contentArray)),
                        contentArray.XxHash64(),
                        Size.FromLong(contentArray.Length)));
                    
                    AddFile(tx, loadout, pluginMod, gamePath);
                }
            }

            await FileStore.BackupFiles(filesToBackup);
            await tx.Commit();
        }
        
        Refresh(ref loadout);

        var diagnostics = await emitter.Diagnose(loadout, CancellationToken.None).ToListAsync();
        var diagnostic = diagnostics.OfType<Diagnostic<Diagnostics.MissingModWithKnownNexusUriMessageData>>();
        diagnostic.Should().ContainSingle();
        
        // Install the dependency and the diagnostic should disappear
        {
            using var tx = Connection.BeginTransaction();
            var dependencyMod = AddEmptyGroup(tx, loadout, "DependencyMod");
            foreach (var dependencyPath in emitter.DependencyPaths)
            {
                var gamePath = new GamePath(dependencyPath.LocationId, dependencyPath.Path);
                AddFile(tx, loadout, dependencyMod, gamePath);
            }
            await tx.Commit();
        }
        
        Refresh(ref loadout);
        
        diagnostics = await emitter.Diagnose(loadout, CancellationToken.None).ToListAsync();
        diagnostics.Should().BeEmpty();
        
        // Disable the dependency and the diagnostic should reappear
        {
            var dependencyMod = LoadoutItemGroup.Load(Connection.Db, loadout.Items.First(m => m.Name == "DependencyMod").Id);
            using var tx = Connection.BeginTransaction();
            tx.Add(dependencyMod, LoadoutItem.Disabled, Null.Instance);
            await tx.Commit();
        }
        
        Refresh(ref loadout);
        
        diagnostics = await emitter.Diagnose(loadout, CancellationToken.None).ToListAsync();
        diagnostics.OfType<Diagnostic<Diagnostics.MissingModWithKnownNexusUriMessageData>>().Should().BeEmpty();
        diagnostics.OfType<Diagnostic<Diagnostics.DisabledGroupDependencyMessageData>>().Should().ContainSingle();
    }
}
