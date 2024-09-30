using ActiveAgents.Grains.States;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.Abstraction;

public interface ITransferProcessingStatlessGrain : IGrainWithIntegerKey
{
    Task<IEnumerable<TransactionState>> Transfer(Guid from, Guid to, decimal amount);
}
