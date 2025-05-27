using NexusMods.Paths;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into an absolute path
/// </summary>
internal class AbsolutePathParser : IOptionParser<AbsolutePath>
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="fileSystem"></param>
    public AbsolutePathParser(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }


    public bool TryParse(string toParse, out AbsolutePath value, out string error)
    {
        try
        {
            value = _fileSystem.FromUnsanitizedFullPath(toParse);
            error = string.Empty;
            return true;
        }
        catch (Exception e)
        {
            value = default!;
            error = e.Message;
            return false;
        }
    }
}
