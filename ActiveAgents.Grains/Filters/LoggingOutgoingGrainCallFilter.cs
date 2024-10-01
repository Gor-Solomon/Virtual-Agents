using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.Filters;

public class LoggingOutgoingGrainCallFilter : IOutgoingGrainCallFilter
{
    private readonly ILogger<LoggingOutgoingGrainCallFilter> _logger;

    public LoggingOutgoingGrainCallFilter(ILogger<LoggingOutgoingGrainCallFilter> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        string message = $"Outgoing Silo Grain Filter: Recvied grain call on {context.Grain} to {context.MethodName} method.";
        _logger.LogInformation(message);

        await context.Invoke();
    }
}
