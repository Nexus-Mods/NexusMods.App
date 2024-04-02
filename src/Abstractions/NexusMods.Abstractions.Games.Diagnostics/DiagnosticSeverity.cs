using JetBrains.Annotations;

namespace NexusMods.Abstractions.Diagnostics;

/// <summary>
/// Describes how severe a diagnostic is.
/// </summary>
[PublicAPI]
public enum DiagnosticSeverity : byte
{
    /// <summary>
    /// Diagnostics should not be created with this severity as they will be hidden.
    /// This only exists in case that conversions and/or serializations fail.
    /// </summary>
    Hidden = 0,

    /// <summary>
    /// Something that doesn't indicate a problem, and offers improvements to the user.
    /// </summary>
    /// <remarks>
    /// This severity can be used to provide helpful advices to the user. Applying these
    /// suggestions MUST NOT introduce further diagnostics of a higher severity. Furthermore,
    /// analysis and deduction MUST BE objective and based on facts. Suggestions have to
    /// be documented, and their improvements must be justifiable. Don't offer improvements
    /// if you aren't sure that the user benefits from them.
    /// </remarks>
    /// <example>
    /// <list type="bullet">
    ///     <item>
    ///         After analysis of the installed mods and their files, packing them into
    ///         an archive, instead of having them as loose files, will improve load times.
    ///     </item>
    /// </list>
    /// </example>
    Suggestion = 1,

    /// <summary>
    /// Something that has an unintended adverse effect on any part of the game.
    /// </summary>
    /// <remarks>
    /// This severity encompasses unintended problems that negatively impact the game
    /// in any of its aspects. This includes the visuals, the performance, and even the gameplay.
    /// </remarks>
    /// <example>
    /// <list type="bullet">
    ///     <item>
    ///         Missing assets that don't crash the game but manifest themselves as a noticeable abnormality.
    ///         As an example, missing textures will get replaced with a default duo color checker-pattern, or a single
    ///         color texture.
    ///     </item>
    /// </list>
    /// </example>
    Warning = 2,

    /// <summary>
    /// Something that will make the game unplayable.
    /// </summary>
    /// <remarks>
    /// Unplayable, in this context, means that the user is unable
    /// to interact with the game. The most common effect of such
    /// problems is a crash. The frequency or likelihood of such
    /// causes or conditions are irrelevant. A crash that happens
    /// when the user starts the game, and a crash that happens
    /// when the user is in a very specific situation, are equal
    /// in severity.
    /// </remarks>
    /// <example>
    /// <list type="bullet">
    ///     <item>A crash.</item>
    ///     <item>A prolonged or indefinite loading screen.</item>
    ///     <item>A prolonged or indefinite freeze.</item>
    /// </list>
    /// </example>
    Critical = 3,
}
