using Orleans;
using System;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.Abstraction;

[Alias("ActiveAgents.Grains.Abstraction.ICheckingAccountGrain")]
public interface ICheckingAccountGrain : IGrainWithGuidKey
{
    Task Initialize(decimal opeeningBalance);

    Task<decimal> GetBalance();

    Task Debit(decimal amount);

    Task Credit(decimal amount);

    Task AddReccuringPayment(Guid id, decimal amount, int frequncyInMinuets);
}