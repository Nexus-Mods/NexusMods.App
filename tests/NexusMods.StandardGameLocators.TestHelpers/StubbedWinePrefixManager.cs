using GameFinder.Common;
using GameFinder.Wine;
using NexusMods.Paths;
using OneOf;

namespace NexusMods.StandardGameLocators.TestHelpers;

public class StubbedWinePrefixManager<TPrefix> : IWinePrefixManager<TPrefix>
    where TPrefix : AWinePrefix
{
    private readonly TPrefix _prefix;

    public StubbedWinePrefixManager(
        TemporaryFileManager manager,
        Func<TemporaryFileManager, TPrefix> factory)
    {
        _prefix = factory(manager);
    }

    public IEnumerable<OneOf<TPrefix, ErrorMessage>> FindPrefixes()
    {
        yield return _prefix;
    }
}
