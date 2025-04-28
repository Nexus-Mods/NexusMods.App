using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// A collection of sections to use.
/// </summary>
[PublicAPI]
public static class Sections
{
    public static readonly SectionId General = SectionId.From(Guid.Parse("3106487b-db84-4caa-acdd-9428506fbf6d"));

    public static readonly SectionId Privacy = SectionId.From(Guid.Parse("4ef7b142-8f0e-4cae-867f-a58d985241c0"));

    public static readonly SectionId Advanced = SectionId.From(Guid.Parse("1531efa7-cb0a-4463-8a04-a865d848ca06"));

    public static readonly SectionId DeveloperTools = SectionId.From(Guid.Parse("c33fb41c-7dc5-4911-b48c-3a8c822083d9"));

    public static readonly SectionId Experimental = SectionId.From(Guid.Parse("864495d4-aa30-48a7-b292-d23adce391f1"));

    public static readonly SectionId GameSpecific = SectionId.From(Guid.Parse("16eb49ba-e1c2-4319-8219-005806759203"));
}
