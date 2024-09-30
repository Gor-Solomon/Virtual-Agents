using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.Abstraction;

[Alias("ActiveAgents.Grains.Abstraction.ICustomerGrain")]
public interface ICustomerGrain : IGrainWithGuidKey, IGrainObserver
{
    [Alias("AddAccount")]
    Task<Guid> CreateAccount(decimal balance);

    [Alias("GetNetWorth")]
    Task<(string name, decimal netWorth, List<Guid> accounts)> GetNetWorth();
    
    [Alias("Initialize")]
    Task Initialize(string fullName);
}
