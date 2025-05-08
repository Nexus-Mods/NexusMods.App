using JetBrains.Annotations;
using TransparentValueObjects;

namespace NexusMods.Abstractions.GuidedInstallers.ValueObjects;

/// <summary>
/// Represents a unique identifier of an <see cref="GuidedInstallationStep"/>.
/// </summary>
[PublicAPI]
[ValueObject<Guid>]
public readonly partial struct StepId { }
