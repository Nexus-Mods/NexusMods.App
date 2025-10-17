using System.Runtime.InteropServices;
using System.Text;
using CliWrap;
using DynamicData.Kernel;
using JetBrains.Annotations;

namespace NexusMods.Sdk;

[PublicAPI]
public abstract class ExecutableRuntimeDependency : IRuntimeDependency
{
    /// <inheritdoc/>
    public abstract string DisplayName { get; }
    /// <inheritdoc/>
    public abstract string Description { get; }
    /// <inheritdoc/>
    public abstract Uri Homepage { get; }
    /// <inheritdoc/>
    public abstract OSPlatform[] SupportedPlatforms { get; }

    /// <inheritdoc/>
    public RuntimeDependencyType DependencyType => RuntimeDependencyType.Executable;

    /// <summary>
    /// Process factory.
    /// </summary>
    protected readonly IProcessRunner ProcessRunner;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected ExecutableRuntimeDependency(IProcessRunner runner)
    {
        ProcessRunner = runner;
    }

    private Optional<Optional<RuntimeDependencyInformation>> _cachedInformation;

    /// <inheritdoc/>
    public ValueTask<Optional<RuntimeDependencyInformation>> QueryInstallationInformation(CancellationToken cancellationToken)
    {
        if (_cachedInformation.HasValue) return ValueTask.FromResult(_cachedInformation.Value);
        return QueryInstallationInformationImpl(cancellationToken);
    }

    protected abstract Command BuildQueryCommand(PipeTarget outputPipeTarget);

    protected abstract RuntimeDependencyInformation ToInformation(ReadOnlySpan<char> output);

    protected virtual async ValueTask<Optional<RuntimeDependencyInformation>> QueryInstallationInformationImpl(CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        var outputPipeTarget = PipeTarget.ToStringBuilder(sb);
        var command = BuildQueryCommand(outputPipeTarget);

        var result = await ProcessRunner.RunAsync(command, cancellationToken: cancellationToken);
        if (!result.IsSuccess)
        {
            _cachedInformation = Optional<Optional<RuntimeDependencyInformation>>.None;
            return Optional<RuntimeDependencyInformation>.None;
        }

        var information = ToInformation(sb.ToString().AsSpan());
        _cachedInformation = Optional<Optional<RuntimeDependencyInformation>>.Create(information);
        return information;
    }
}
