using AutoFixture;
using JetBrains.Annotations;

namespace NexusMods.Paths.TestingHelpers;

/// <summary>
/// Extension functions to add customization to <see cref="Fixture"/>.
/// </summary>
[PublicAPI]
public static class Customizations
{
    /// <summary>
    /// Registers factories for the following types:
    /// <list type="bullet">
    ///     <item><see cref="IFileSystem"/></item>
    ///     <item><see cref="InMemoryFileSystem"/></item>
    ///     <item><see cref="AbsolutePath"/></item>
    ///     <item><see cref="RelativePath"/></item>
    /// </list>
    /// </summary>
    /// <param name="fixture">The provided <see cref="Fixture"/> to use.</param>
    public static void AddFileSystemCustomizations(this Fixture fixture)
    {
        fixture.Customize<InMemoryFileSystem>(composer =>
            composer.FromFactory(() => new InMemoryFileSystem()));

        fixture.Customize<IFileSystem>(composer =>
            composer.FromFactory(() => new InMemoryFileSystem()));

        fixture.Customize<AbsolutePath>(composer =>
            composer.FromFactory<IFileSystem, string>((fs, path) =>
            {
                var fullPath = OperatingSystem.IsWindows()
                    ? $"C:\\{path}"
                    : $"/{path}";
                return fs.FromFullPath(fullPath);
            }));

        fixture.Customize<RelativePath>(composer =>
            composer.FromFactory<string>(path => new RelativePath(path)));
    }
}
