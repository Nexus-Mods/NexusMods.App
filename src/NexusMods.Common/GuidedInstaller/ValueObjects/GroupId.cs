using JetBrains.Annotations;
using Vogen;

namespace NexusMods.Common.GuidedInstaller.ValueObjects;

/// <summary>
/// Represents a unique identifier of an <see cref="OptionGroup"/>.
/// </summary>
[PublicAPI]
[ValueObject<Guid>]
public readonly partial struct GroupId { }
