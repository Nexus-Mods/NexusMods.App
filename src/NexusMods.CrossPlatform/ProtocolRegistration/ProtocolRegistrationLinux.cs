using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Logging;
using NexusMods.App.BuildInfo;
using NexusMods.CrossPlatform.Process;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.ProtocolRegistration;

/// <summary>
/// Protocol registration for Linux.
/// </summary>
[SupportedOSPlatform("linux")]
internal class ProtocolRegistrationLinux : IProtocolRegistration
{
    private readonly ILogger _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ProtocolRegistrationLinux(ILogger<ProtocolRegistrationLinux> logger)
    {
        _logger = logger;
    }

    public Task RegisterHandler(string uriScheme, bool setAsDefaultHandler = true, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
