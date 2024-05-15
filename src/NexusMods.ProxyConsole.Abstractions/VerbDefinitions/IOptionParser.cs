namespace NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

/// <summary>
/// Defines a parser for a specific option type.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IOptionParser<T>
{
    /// <summary>
    /// Parses the value into the option type. If the value is invalid, the error message should be set
    /// and <c>false</c> should be returned. Otherwise, <c>true</c> should be returned.
    /// </summary>
    /// <param name="toParse"></param>
    /// <param name="value"></param>
    /// <param name="error"></param>
    /// <returns></returns>
    bool TryParse(string toParse, out T value, out string error);
}
