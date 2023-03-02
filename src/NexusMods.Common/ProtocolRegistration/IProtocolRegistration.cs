namespace NexusMods.Common.ProtocolRegistration;

/// <summary>
/// deals with protocol registration, that is: setting up this application system wide
/// as the default handler for a custom protocol (e.g. nxm://...)
/// For the actual code to be invoked when such a url is received, see NexusMods.Cli.ProtocolInvokation
///
/// This is platform dependent functionality
/// </summary>
public interface IProtocolRegistration
{
    /// <summary>
    /// register this application as a handler for a protocol (e.g. nxm://...).
    /// This should be called every time the application runs for every protocol it handles
    /// </summary>
    /// <param name="protocol">The protocol to register for</param>
    /// <returns>the previous handler, if any</returns>
    string RegisterSelf(string protocol);

    /// <summary>
    /// register an arbitrary command line as the handler for a protocol.
    /// The primary usecase for this is to unregister the current application, potentially
    /// restoring a previous handler
    /// </summary>
    /// <param name="protocol">The protocol to register for</param>
    /// <param name="friendlyName">Arbitrary friendly name for the protocol</param>
    /// <param name="commandLine"></param>
    /// <returns>the previous handler, if any</returns>
    string Register(string protocol, string friendlyName, string? commandLine = null);

    /// <summary>
    /// determine if this application is the handler for a protocol. This is based on the full url
    /// of the calling process so another installation of the same application would _not_ count
    /// </summary>
    /// <param name="protocol">The protocol to check for</param>
    bool IsSelfHandler(string protocol);
}
