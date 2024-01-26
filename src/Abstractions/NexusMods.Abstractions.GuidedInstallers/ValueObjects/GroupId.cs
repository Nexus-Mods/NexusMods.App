using JetBrains.Annotations;
using TransparentValueObjects;

namespace NexusMods.Abstractions.GuidedInstallers.ValueObjects;

/// <summary>
/// Represents a unique identifier of an <see cref="OptionGroup"/>.
/// </summary>
[PublicAPI]
[ValueObject<Guid>]
public readonly partial struct GroupId;
