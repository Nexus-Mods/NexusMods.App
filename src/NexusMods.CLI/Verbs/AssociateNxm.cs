using NexusMods.Common.ProtocolRegistration;

namespace NexusMods.CLI.Verbs;

/// <summary>
///     Associates an NXM handler with the current application.
/// </summary>
public class AssociateNxm : AVerb
{
    private readonly IProtocolRegistration _protocolRegistration;

    /// <summary/>
    public AssociateNxm(IProtocolRegistration protocolRegistration) => _protocolRegistration = protocolRegistration;

    public static VerbDefinition Definition { get; } = new("associate-nxm",
        "Associates NXM links with Nexus App.", Array.Empty<OptionDefinition>());

    /// <inheritdoc />
    public async Task<int> Run(CancellationToken token)
    {
        await _protocolRegistration.RegisterSelf("nxm");
        return 0;
    }
}
