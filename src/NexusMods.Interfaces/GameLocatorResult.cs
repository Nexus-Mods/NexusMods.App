using NexusMods.Paths;

namespace NexusMods.Interfaces;

public record GameLocatorResult(AbsolutePath Path, Version? Version = null);