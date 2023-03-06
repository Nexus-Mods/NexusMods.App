namespace NexusMods.CLI.OptionParsers;

public interface IOptionParser<T>
{
    public T Parse(string input, OptionDefinition<T> definition);
    public IEnumerable<string> GetOptions(string input);
}
