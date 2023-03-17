using AutoFixture;
using AutoFixture.Xunit2;
using JetBrains.Annotations;

namespace NexusMods.Paths.TestingHelpers;

/// <summary>
/// Custom <see cref="AutoDataAttribute"/> with support for types from
/// <c>NexusMods.Paths</c>.
/// </summary>
[PublicAPI]
public class AutoFileSystemAttribute : AutoDataAttribute
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public AutoFileSystemAttribute() : base(() =>
    {
        var ret = new Fixture();

        ret.AddFileSystemCustomizations();

        return ret;
    }) { }
}
