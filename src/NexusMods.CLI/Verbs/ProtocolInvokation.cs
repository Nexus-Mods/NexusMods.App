using Microsoft.Extensions.Logging;
using NexusMods.CLI;
using NexusMods.CLI.Types;
using NexusMods.Common;
using NexusMods.DataModel.Interprocess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusMods.CLI.Verbs;

public class ProtocolInvokation : AVerb<string>
{
    public static VerbDefinition Definition => new("protocol-invoke",
        "handle a url with custom protocol",
        new OptionDefinition[]
        {
            new OptionDefinition<string>("u", "url", "The url to handle")
        });

    private IEnumerable<IProtocolHandler> _handlers;
    private ILogger<ProtocolInvokation> _logger;

    public ProtocolInvokation(ILogger<ProtocolInvokation> logger, IEnumerable<IProtocolHandler> handlers)
    {
        _logger = logger;
        _handlers = handlers;

        logger.LogInformation("number of handlers: {}", handlers.Count());
    }

    public async Task<int> Run(string url, CancellationToken cancel)
    {
        var uri = new Uri(url);
        var handler = _handlers.FirstOrDefault(iter => iter.Protocol == uri.Scheme);
        if (handler == null)
        {
            throw new Exception($"Unsupported protocol \"{uri.Scheme}\"");
        }

        await handler.Handle(url, cancel);

        return 0;
    }
}
