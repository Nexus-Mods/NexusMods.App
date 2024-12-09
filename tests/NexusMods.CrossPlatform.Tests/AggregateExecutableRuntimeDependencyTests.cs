using System.Runtime.InteropServices;
using CliWrap;
using DynamicData.Kernel;
using FluentAssertions;
using NexusMods.CrossPlatform.Process;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.Tests;

public class AggregateExecutableRuntimeDependencyTests
{
    private const string TestDisplayName = "Test Dependency";
    private const string TestDescription = "A test dependency";
    private static readonly Uri TestUri = new("https://www.youtube.com/watch?v=o-YBDTqX_ZU");
    
    private readonly IProcessFactory _processFactory;
    private readonly FakeExecutableDependency _dependency1;
    private readonly FakeExecutableDependency _dependency2;
    private readonly AggregateExecutableRuntimeDependency _sut;

    public AggregateExecutableRuntimeDependencyTests()
    {
        _processFactory = new FakeProcessFactory(0);
        _dependency1 = new FakeExecutableDependency(_processFactory, [OSPlatform.Windows]);
        _dependency2 = new FakeExecutableDependency(_processFactory, [OSPlatform.Linux]);
        
        _sut = new AggregateExecutableRuntimeDependency(
            TestDisplayName,
            TestDescription,
            TestUri,
            [_dependency1, _dependency2]);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenNoDependenciesProvided()
    {
        var act = () => new AggregateExecutableRuntimeDependency(
            TestDisplayName,
            TestDescription,
            TestUri,
            []);

        act.Should().Throw<ArgumentException>()
            .WithMessage("At least one dependency must be provided*");
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties_WithProvidedValues()
    {
        _sut.DisplayName.Should().Be(TestDisplayName);
        _sut.Description.Should().Be(TestDescription);
        _sut.Homepage.Should().Be(TestUri);
        _sut.DependencyType.Should().Be(RuntimeDependencyType.Executable);
    }

    [Fact]
    public void SupportedPlatforms_ShouldCombineAndDeduplicate_PlatformsFromAllDependencies()
    {
        var dep1 = new FakeExecutableDependency(_processFactory, [OSPlatform.Windows, OSPlatform.Linux]);
        var dep2 = new FakeExecutableDependency(_processFactory, [OSPlatform.Linux, OSPlatform.OSX]);

        var aggregate = new AggregateExecutableRuntimeDependency(
            TestDisplayName,
            TestDescription,
            TestUri,
            [dep1, dep2]);

        aggregate.SupportedPlatforms.Should()
            .BeEquivalentTo([OSPlatform.Windows, OSPlatform.Linux, OSPlatform.OSX]);
    }

    [Fact]
    public async Task QueryInstallationInformation_ShouldReturnNone_WhenNoDependencyIsAvailable()
    {
        _dependency1.SetAvailable(false);
        _dependency2.SetAvailable(false);

        var result = await _sut.QueryInstallationInformation(CancellationToken.None);

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task QueryInstallationInformation_ShouldReturnFirstAvailableDependency()
    {
        var dep1 = new FakeExecutableDependency(_processFactory, [OSPlatform.Windows, OSPlatform.Linux, OSPlatform.OSX]);
        var dep2 = new FakeExecutableDependency(_processFactory, [OSPlatform.Windows, OSPlatform.Linux, OSPlatform.OSX]);

        var aggregate = new AggregateExecutableRuntimeDependency(
            TestDisplayName,
            TestDescription,
            TestUri,
            [dep1, dep2]);
        
        var expectedInfo = new RuntimeDependencyInformation 
        { 
            RawVersion = "1.0.0",
            Version = new Version(1, 0, 0),
        };
        
        dep1.SetAvailable(false);
        dep2.SetAvailable(true, expectedInfo);

        var result = await aggregate.QueryInstallationInformation(CancellationToken.None);

        result.HasValue.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedInfo);
    }

    [Fact]
    public async Task QueryInstallationInformation_ShouldCacheActiveDependency()
    {
        // Create a dependency that supports all platforms
        var dependency1 = new FakeExecutableDependency(
            _processFactory, 
            [OSPlatform.Windows, OSPlatform.Linux, OSPlatform.OSX]);
        var dependency2 = new FakeExecutableDependency(
            _processFactory, 
            [OSPlatform.Windows, OSPlatform.Linux, OSPlatform.OSX]);

        var sut = new AggregateExecutableRuntimeDependency(
            TestDisplayName,
            TestDescription,
            TestUri,
            [dependency1, dependency2]);

        dependency1.SetAvailable(false);
        dependency2.SetAvailable(true);

        // First call should query both dependencies since they support the current platform
        var firstResult = await sut.QueryInstallationInformation(CancellationToken.None);
    
        dependency1.QueryCount.Should().BeGreaterThan(0);
        dependency2.QueryCount.Should().BeGreaterThan(0);
        firstResult.HasValue.Should().BeTrue();

        var dep1Count = dependency1.QueryCount;
        var dep2Count = dependency2.QueryCount;

        // Second call should use cached active dependency
        var secondResult = await sut.QueryInstallationInformation(CancellationToken.None);
    
        dependency1.QueryCount.Should().Be(dep1Count);  // Should not be queried again
        dependency2.QueryCount.Should().BeGreaterThan(dep2Count); // Should be queried again since it was active
        secondResult.HasValue.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableDependencies_ShouldReturnAllAvailableDependencies()
    {
        _dependency1.SetAvailable(true);
        _dependency2.SetAvailable(true);

        var result = await _sut.GetAvailableDependenciesAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(_dependency1);
        result.Should().Contain(_dependency2);
    }

    [Fact]
    public async Task GetAvailableDependencies_ShouldHandleFailingDependencies()
    {
        _dependency1.SetThrowException(true);
        _dependency2.SetAvailable(true);

        var result = await _sut.GetAvailableDependenciesAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        result.Should().Contain(_dependency2);
    }
    
    [Fact]
    public async Task QueryInstallationInformation_ShouldSkipDependenciesThatDontSupportCurrentOS()
    {
        // Create dependencies with specific platform support
        var windowsDep = new FakeExecutableDependency(_processFactory, [OSPlatform.Windows]);
        var linuxDep = new FakeExecutableDependency(_processFactory, [OSPlatform.Linux]);
        var macOSDep = new FakeExecutableDependency(_processFactory, [OSPlatform.OSX]);
    
        windowsDep.SetAvailable(true);
        linuxDep.SetAvailable(true);
        macOSDep.SetAvailable(true);

        var aggregate = new AggregateExecutableRuntimeDependency(
            TestDisplayName,
            TestDescription,
            TestUri,
            [windowsDep, linuxDep, macOSDep]);

        var result = await aggregate.QueryInstallationInformation(CancellationToken.None);

        // Verify that only the dependency matching the current OS is considered
        if (OSInformation.Shared.Platform == OSPlatform.Windows)
        {
            windowsDep.QueryCount.Should().BeGreaterThan(0);
            linuxDep.QueryCount.Should().Be(0);
            macOSDep.QueryCount.Should().Be(0);
            result.HasValue.Should().BeTrue();
        }
        else if (OSInformation.Shared.Platform == OSPlatform.Linux)
        {
            windowsDep.QueryCount.Should().Be(0);
            linuxDep.QueryCount.Should().BeGreaterThan(0);
            macOSDep.QueryCount.Should().Be(0);
            result.HasValue.Should().BeTrue();
        }
        else if (OSInformation.Shared.Platform == OSPlatform.OSX)
        {
            windowsDep.QueryCount.Should().Be(0);
            linuxDep.QueryCount.Should().Be(0);
            macOSDep.QueryCount.Should().BeGreaterThan(0);
            result.HasValue.Should().BeTrue();
        }
        else
        {
            // On other platforms, no dependencies should be available
            windowsDep.QueryCount.Should().Be(0);
            linuxDep.QueryCount.Should().Be(0);
            macOSDep.QueryCount.Should().Be(0);
            result.HasValue.Should().BeFalse();
        }
    }

    private class FakeExecutableDependency : ExecutableRuntimeDependency
    {
        private readonly OSPlatform[] _platforms;
        private bool _isAvailable;
        private bool _throwException;
        private RuntimeDependencyInformation? _information;
        
        public int QueryCount { get; private set; }

        public override string DisplayName => "Fake Dependency";
        public override string Description => "A fake dependency for testing";
        public override Uri Homepage => new("https://example.com");
        public override OSPlatform[] SupportedPlatforms => _platforms;

        public FakeExecutableDependency(IProcessFactory processFactory, OSPlatform[] platforms) 
            : base(processFactory)
        {
            _platforms = platforms;
        }

        public void SetAvailable(bool isAvailable, RuntimeDependencyInformation? information = null)
        {
            _isAvailable = isAvailable;
            _information = information ?? new RuntimeDependencyInformation();
        }

        public void SetThrowException(bool throwException)
        {
            _throwException = throwException;
        }

        protected override async ValueTask<Optional<RuntimeDependencyInformation>> QueryInstallationInformationImpl(
            CancellationToken cancellationToken)
        {
            QueryCount++;
            if (_throwException)
                throw new Exception("Simulated failure");

            if (!_isAvailable)
                return Optional<RuntimeDependencyInformation>.None;

            return _information!;
        }

        protected override Command BuildQueryCommand(PipeTarget outputPipeTarget) => throw new NotImplementedException();
        protected override RuntimeDependencyInformation ToInformation(ReadOnlySpan<char> output) => throw new NotImplementedException();
    }
}
