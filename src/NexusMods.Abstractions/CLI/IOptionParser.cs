namespace NexusMods.Abstractions.CLI;

/// <summary>
/// A service that parses a string into a C# type for use with CLI inputs
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IOptionParser<T>
{
    /// <summary>
    /// Performs the parsing of the input string into the type
    /// </summary>
    /// <param name="input"></param>
    /// <param name="definition"></param>
    /// <returns></returns>
    public T Parse(string input, OptionDefinition<T> definition);

    /// <summary>
    /// Get various options that match the input string, used for tab completion
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public IEnumerable<string> GetOptions(string input);
}
