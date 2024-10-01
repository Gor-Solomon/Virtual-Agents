using ActiveAgents.Grains.Abstraction;
using ActiveAgents.Grains.States;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Transactions.Abstractions;
using System;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.Grains;

[Reentrant]
public class AtmGrain : Grain, IAtmGrain, IIncomingGrainCallFilter
{
    private readonly ILogger<AtmGrain> _logger;
    private readonly ITransactionalState<AtmState> _atmState;

    public AtmGrain(
        ILogger<AtmGrain> logger,
        [TransactionalState(nameof(AtmState))] ITransactionalState<AtmState> atmState)
    {
        _logger = logger;
        _atmState = atmState;
    }

    public async Task Initialise(decimal oppeningBalance)
    {
        await _atmState.PerformUpdate(s =>
        {
            s.Id = this.GetGrainId().GetGuidKey();
            s.Balance = oppeningBalance;
        });
    }

    public async Task Withdraw(Guid accountId, decimal amount)
    {
        var existingBalance = await _atmState.PerformRead(s => s.Balance);

        if (existingBalance - amount < 0)
        {
            throw new InvalidOperationException("Rejected, Out Of Cash...");
        }

        await _atmState.PerformUpdate(s =>
        {
            s.Balance -= amount;
        });
    }

    public async Task<decimal> CheckCashe()
    {
        return await _atmState.PerformRead(s => s.Balance);
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        string message = $"Incoming ATM Grain Filter: Recived grain call on {context.Grain} to {context.MethodName} method";
        _logger.LogInformation(message);

        await context.Invoke();
    }
}