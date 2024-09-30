using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.Events;

[GenerateSerializer]
[Alias("ActiveAgents.Grains.Events.BalanceChangeEvent")]
public record BalanceChangeEvent
{
    [Id(0)]
    public Guid AccountId { get; init; }

    [Id(1)]
    public decimal Balance { get; init; }
}
