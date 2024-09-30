using ActiveAgents.Grains.Abstraction;
using ActiveAgents.Grains.States;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.Grains;

[StatelessWorker(3)]
public class TransferProcessingStatlessGrain : Grain, ITransferProcessingStatlessGrain
{
    private int _counter;
    private readonly ConcurrentQueue<TransactionState> _transactions;
    private readonly ITransactionClient _transactionClient;

    public TransferProcessingStatlessGrain(ITransactionClient transactionClient)
    {
        _transactionClient = transactionClient;
        _transactions = [];
    }

    public async Task<IEnumerable<TransactionState>> Transfer(Guid from, Guid to, decimal amount)
    {
        var fromGrain = GrainFactory.GetGrain<IAccountGrain>(from);
        var toGrain = GrainFactory.GetGrain<IAccountGrain>(to);

        await _transactionClient.RunTransaction(TransactionOption.Create, async () =>
        {
            await fromGrain.Debit(amount);
            await toGrain.Credit(amount);
        });

        var trans = new TransactionState()
        {
            From = from,
            To = to,
            Amount = amount,
            Timestamp = DateTime.UtcNow,
            Purpose = _counter++.ToString(),
        };

        _transactions.Enqueue(trans);

        return _transactions;
    }
}
