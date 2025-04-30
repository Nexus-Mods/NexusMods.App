using NexusMods.ProxyConsole.Abstractions.Implementations;

namespace NexusMods.Abstractions.Cli;

/// <summary>
///     Used for creating items which can be rendered to CLI.
/// </summary>
public static class Renderable
{
    /// <summary>
    /// Creates a new <see cref="Text"/> renderable.
    /// </summary>
    /// <param name="template"></param>
    /// <returns></returns>
    public static Text Text(string template) => new Text { Template = template };

    /// <summary>
    /// Creates a new <see cref="Text"/> renderable out of the given arguments and template.
    /// </summary>
    /// <param name="template"></param>
    /// <returns></returns>
    public static Text Text(string template, string[] args) => new Text { Template = template, Arguments = args};
    
    /// <summary>
    /// Creates a new <see cref="Text"/> renderable with a new line at the end.
    /// </summary>
    public static Text TextLine(string template) => new Text { Template = template + "\n" };

}
