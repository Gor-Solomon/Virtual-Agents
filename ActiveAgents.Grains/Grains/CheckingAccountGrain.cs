using ActiveAgents.Grains.Abstraction;
using ActiveAgents.Grains.States;
using Orleans;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.Grains;

public class CheckingAccountGrain : Grain, ICheckingAccountGrain
{
    private readonly IPersistentState<BalanceState> _balanceState;
    private readonly IPersistentState<CheckingAccountState> _checkingAccountState;

    public CheckingAccountGrain(
        [PersistentState(nameof(BalanceState), "tableStorage")] IPersistentState<BalanceState> balanceState,
        [PersistentState(nameof(CheckingAccountState), "blobStorage")] IPersistentState<CheckingAccountState> checkingAccountState)
    {
        _balanceState = balanceState;
        _checkingAccountState = checkingAccountState;
    }

    public async Task Credit(decimal amount)
    {
        _balanceState.State.Balance += amount;
        await _balanceState.WriteStateAsync();
    }

    public async Task Debit(decimal amount)
    {
        _balanceState.State.Balance -= amount;
        await _balanceState.WriteStateAsync();
    }

    public async Task<decimal> GetBalance() => _balanceState.State.Balance;

    public async Task Initialize(decimal opeeningBalance)
    {
        _balanceState.State.Balance = opeeningBalance;
        await _balanceState.WriteStateAsync();

        _checkingAccountState.State.OpenedAtUtc = DateTime.UtcNow;
        _checkingAccountState.State.AccountType = "Default";
        _checkingAccountState.State.AccountId = this.GetGrainId().GetGuidKey();
        await _checkingAccountState.WriteStateAsync();
    }
}