using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.Abstraction;

[Alias("ActiveAgents.Grains.Abstraction.IAtmGrain")]
public interface IAtmGrain : IGrainWithGuidKey
{
    
    [Transaction(TransactionOption.Create)]
    [Alias("Initialise")]
    public Task Initialise(decimal oppeningBalance);

    [Transaction(TransactionOption.CreateOrJoin)]
    [Alias("Withdraw")]
    public Task Withdraw(Guid accountId, decimal amount);

    [Transaction(TransactionOption.Create)]
    [Alias("CheckCashe")]
    Task<decimal> CheckCashe();
}
