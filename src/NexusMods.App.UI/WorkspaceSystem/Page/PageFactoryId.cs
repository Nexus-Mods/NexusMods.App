using TransparentValueObjects;

namespace NexusMods.App.UI.WorkspaceSystem;

[ValueObject<Guid>]
public readonly partial struct PageFactoryId : IAugmentWith<JsonAugment> { }
