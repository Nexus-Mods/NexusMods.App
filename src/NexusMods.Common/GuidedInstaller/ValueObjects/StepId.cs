using JetBrains.Annotations;
using Vogen;

namespace NexusMods.Common.GuidedInstaller.ValueObjects;

/// <summary>
/// Represents a unique identifier of an <see cref="GuidedInstallationStep"/>.
/// </summary>
[PublicAPI]
[ValueObject<Guid>]
public readonly partial struct StepId { }
