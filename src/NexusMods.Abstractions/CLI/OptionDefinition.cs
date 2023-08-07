namespace NexusMods.Abstractions.CLI;

/// <summary>
/// Defines a option the user can pass to the command line
/// </summary>
/// <param name="ShortOption"></param>
/// <param name="LongOption"></param>
/// <param name="Description"></param>
public abstract record OptionDefinition(string ShortOption, string LongOption, string Description)
{

    /// <summary>
    /// Returns the type this option is parsed into
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public abstract Type ReturnType { get; }
}

/// <summary>
/// Defines a option the user can pass to the command line
/// </summary>
/// <param name="ShortOption"></param>
/// <param name="LongOption"></param>
/// <param name="Description"></param>
/// <typeparam name="T"></typeparam>
public record OptionDefinition<T>(string ShortOption, string LongOption, string Description) : OptionDefinition(ShortOption, LongOption, Description)
{
    /// <inheritdoc />
    public override Type ReturnType => typeof(T);
}
