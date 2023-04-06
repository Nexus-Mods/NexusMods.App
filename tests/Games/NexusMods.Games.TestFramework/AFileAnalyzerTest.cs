using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.Games.TestFramework;

[PublicAPI]
public class AFileAnalyzerTest<TGame, TFileAnalyzer> : AGameTest<TGame>
    where TGame : AGame
    where TFileAnalyzer : IFileAnalyzer
{
    protected readonly TFileAnalyzer FileAnalyzer;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected AFileAnalyzerTest(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        FileAnalyzer = serviceProvider.FindImplementationInContainer<TFileAnalyzer, IFileAnalyzer>();
    }

    /// <summary>
    /// Uses <typeparamref name="TFileAnalyzer"/> to analyze the provided
    /// file and returns the analysis data.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    protected async Task<IFileAnalysisData[]> AnalyzeFile(AbsolutePath path)
    {
        await using var stream = FileSystem.ReadFile(path);

        var asyncEnumerable = FileAnalyzer.AnalyzeAsync(new FileAnalyzerInfo
        {
            FileName = path.FileName,
            Stream = stream
        });

        var res = await asyncEnumerable.ToArrayAsync();
        return res;
    }
}
