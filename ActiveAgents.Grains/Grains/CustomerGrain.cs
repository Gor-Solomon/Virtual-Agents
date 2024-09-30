using ActiveAgents.Grains.Abstraction;
using ActiveAgents.Grains.Events;
using ActiveAgents.Grains.States;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.Grains;

public class CustomerGrain : Grain, ICustomerGrain, IAsyncObserver<BalanceChangeEvent>
{
    private readonly IPersistentState<CustomerState> _customerState;

    public CustomerGrain([PersistentState(nameof(CustomerState), "tableStorage")] IPersistentState<CustomerState> customerState)
    {
        _customerState = customerState;
    }

    public async override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var streamProvider = this.GetStreamProvider("QueueStreamProvider");

        foreach (var state in _customerState.State.AccountsBalanceRegistery.Keys) 
        {
            var streamId = StreamId.Create("BalanceStream", state);

            var stream = streamProvider.GetStream<BalanceChangeEvent>(streamId);

            var handlers = await stream.GetAllSubscriptionHandles();

            foreach (var handler in handlers)
            {
                await handler.ResumeAsync(this);
            }
        }

        await _customerState.WriteStateAsync();
    }

    public async Task<Guid> CreateAccount(decimal openingBalance)
    {
        Guard();

        var accountId = Guid.NewGuid();

        var accountGrain = GrainFactory.GetGrain<IAccountGrain>(accountId);
        await accountGrain.Initialize(openingBalance);

        _customerState.State.AccountsBalanceRegistery.Add(accountId, openingBalance);

        var streamProvider = this.GetStreamProvider("QueueStreamProvider");

        var streamId = StreamId.Create("BalanceStream", accountId);

        var stream = streamProvider.GetStream<BalanceChangeEvent>(streamId);

        await stream.SubscribeAsync(this);

        await _customerState.WriteStateAsync();

        return accountId;
    }

    public async Task<(string name, decimal netWorth, List<Guid> accounts)> GetNetWorth()
    {
        Guard();

        return (_customerState.State.FullName,
                                _customerState.State.AccountsBalanceRegistery.Values.Sum(),
                                _customerState.State.AccountsBalanceRegistery.Keys.ToList());
    }

    public Task OnCompletedAsync()
    {
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception ex)
    {
        return Task.CompletedTask;
    }

    public async Task OnNextAsync(BalanceChangeEvent item, StreamSequenceToken token = null)
    {
        _customerState.State.AccountsBalanceRegistery.Remove(item.AccountId);
        _customerState.State.AccountsBalanceRegistery.Add(item.AccountId, item.Balance);

        await _customerState.WriteStateAsync();
    }

    public async Task Initialize(string fullName)
    {
        _customerState.State.FullName = fullName;

        await _customerState.WriteStateAsync();
    }

    private void Guard()
    {
        if (string.IsNullOrWhiteSpace(_customerState.State.FullName))
        {
            throw new InvalidOperationException("Customer Does Not Exist...");
        }
    } 
}
