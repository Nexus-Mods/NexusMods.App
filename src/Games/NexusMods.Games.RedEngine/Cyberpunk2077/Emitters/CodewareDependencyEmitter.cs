using System.Text.RegularExpressions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

/// <summary>
/// Codeware is an extension to Cyberpunk that adds two common extension points. We use regexes to detect these
/// uses.
///  * .reds files will often use Codeware by extending ScriptableService, a class provided by Codeware
///  * .lua files will use a global called Codeware
/// </summary>
public partial class CodewareDependencyEmitter : AFileStringContentsDependencyEmitter
{
    public CodewareDependencyEmitter(IFileStore fileStore) : base(fileStore)
    {
        
    }

    protected override string DependencyName => "Codeware";

    protected internal override IEnumerable<GamePath> DependencyPaths { get; } =
    [
        new GamePath(LocationId.Game, "red4ext/plugins/CodeWare/CodeWare.dll"),
    ];
    protected internal override GamePath[] DependantPaths { get; } = [new GamePath(LocationId.Game, "")];
    protected internal override Extension[] DependantExtensions { get; } = [new(".reds"), new Extension(".lua")];
    protected override GameDomain Domain => Cyberpunk2077Game.StaticDomain;
    protected override ModId ModId { get; } = ModId.From(7780);

    public override Regex[] DependantRegexes { get; } =
    [
        ScriptableServiceRegex(),
        CodewareRegex(),
    ];
    
    [GeneratedRegex(@"extends\s+ScriptableService\s+{")]
    private static partial Regex ScriptableServiceRegex();
    
    [GeneratedRegex(@"\s+Codeware\s+")]
    private static partial Regex CodewareRegex();
}
