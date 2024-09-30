using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.States;

[GenerateSerializer]
[Alias("ActiveAgents.Grains.States.CustomerState")]
public record CustomerState
{
    [Id(0)]
    public string FullName { get; set; }

    [Id(1)]
    public Dictionary<Guid, decimal> AccountsBalanceRegistery { get; set; } = [];
}
