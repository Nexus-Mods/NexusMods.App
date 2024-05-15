using System;

namespace NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

/// <summary>
/// A delegate based parser for options.
/// </summary>
/// <param name="parser"></param>
/// <typeparam name="T"></typeparam>
public class DelegateParser<T>(Func<string, (T? Value, string? Error)> parser) : IOptionParser<T>
{
    /// <inheritdoc />
    public bool TryParse(string toParse, out T value, out string error)
    {
        try
        {
            var (parsedValue, parsedError) = parser(toParse);
            value = parsedValue!;
            error = parsedError!;
            return parsedError is null;
        }
        catch (Exception e)
        {
            value = default!;
            error = e.Message;
            return false;
        }
    }
}
