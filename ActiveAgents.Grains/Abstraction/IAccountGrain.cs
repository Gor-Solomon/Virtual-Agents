using Orleans;
using Orleans.Concurrency;
using System;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.Abstraction;

[Alias("ActiveAgents.Grains.Abstraction.IAccountGrain")]
public interface IAccountGrain : IGrainWithGuidKey
{
    [Transaction(TransactionOption.Create)]
    [Alias("Initialize")]
    Task Initialize(decimal opeeningBalance);

    [Transaction(TransactionOption.Create)]
    [Alias("GetBalance")]
    Task<decimal> GetBalance();

    [Transaction(TransactionOption.CreateOrJoin)]
    [Alias("Debit")]
    Task Debit(decimal amount);

    [Transaction(TransactionOption.CreateOrJoin)]
    [Alias("Credit")]
    Task Credit(decimal amount);

    [Alias("AddReccuringPayment")]
    Task AddReccuringPayment(Guid id, decimal amount, int frequncyInMinuets);

    [OneWay]
    [Alias("FireAndForget")]
    Task FireAndForget();

    Task CancelableWork(long workInSeconds, GrainCancellationToken cancellationToken);
}