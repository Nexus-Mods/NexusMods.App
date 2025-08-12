using JetBrains.Annotations;
using NexusMods.Paths;

namespace NexusMods.Abstractions.GameLocators;

[PublicAPI]
public record struct GameTargetInfo(GameStore Store, IOSInformation OS);
