using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// Delegate based parser for options.
/// </summary>
[PublicAPI]
public class DelegateParser<T> : IOptionParser<T> where T : notnull
{
    /// <summary>
    /// Parsing delegate.
    /// </summary>
    public delegate (T? Value, string? Error) ParseDelegate(string toParse);

    private readonly ParseDelegate _delegate;

    /// <summary>
    /// Constructor.
    /// </summary>
    public DelegateParser(ParseDelegate parseDelegate)
    {
        _delegate = parseDelegate;
    }

    bool IOptionParser<T>.TryParse(string toParse, [NotNullWhen(true)] out T? value, [NotNullWhen(false)] out string? error)
    {
        try
        {
            var (parsedValue, parsedError) = _delegate(toParse);
            value = parsedValue;
            error = parsedError;
            return parsedError is null;
        }
        catch (Exception e)
        {
            value = default(T);
            error = e.Message;
            return false;
        }
    }
}
