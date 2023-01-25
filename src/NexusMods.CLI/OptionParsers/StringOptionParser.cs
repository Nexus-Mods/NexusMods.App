using System.ComponentModel;

namespace NexusMods.CLI.OptionParsers;


/// <summary>
/// A option parser that uses TypeConverters to confer the string to the type.
/// </summary>
/// <typeparam name="T"></typeparam>
public class StringOptionParser<T> : IOptionParser<T>
{
    private readonly TypeConverter _definition;

    public StringOptionParser()
    {
        _definition = TypeDescriptor.GetConverter(typeof(T));

    }
    public T Parse(string input, OptionDefinition<T> definition)
    {
        return (T)_definition.ConvertFrom(input)!;
    }

    public IEnumerable<string> GetOptions(string input)
    {
        return Array.Empty<string>();
    }
}