using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.Filters;

public class LoggingIncomingGrainCallFilter : IIncomingGrainCallFilter
{
    private readonly ILogger<LoggingIncomingGrainCallFilter> _logger;

    public LoggingIncomingGrainCallFilter(ILogger<LoggingIncomingGrainCallFilter> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        string message = $"Incoming Silo Grain Filter: Recvied grain call on {context.Grain} to {context.MethodName} method.";
       // _logger.LogInformation(message);

        await context.Invoke();
    }
}
