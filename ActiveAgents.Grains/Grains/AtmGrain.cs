using ActiveAgents.Grains.Abstraction;
using ActiveAgents.Grains.States;
using Orleans;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.Grains;

public class AtmGrain : Grain, IAtmGrain
{
    private readonly IPersistentState<AtmState> _atmState;

    public AtmGrain([PersistentState(nameof(AtmState), "tableStorage")] IPersistentState<AtmState> atmState)
    {
        _atmState = atmState;
    }

    public async Task Initialise(decimal oppeningBalance)
    {
        _atmState.State.Id = this.GetGrainId().GetGuidKey();
        _atmState.State.Balance = oppeningBalance;
        
        await _atmState.WriteStateAsync();
    }

    public async Task Withdraw(Guid accountId, decimal amount)
    {
        var accountGrain = GrainFactory.GetGrain<ICheckingAccountGrain>(accountId);

        if (_atmState.State.Balance - amount < 0)
        {
            throw new InvalidOperationException("Rejected, Out Of Cash...");
        }
        _atmState.State.Balance -= amount;
        await accountGrain.Debit(amount);

        await _atmState.WriteStateAsync();
    }
}
