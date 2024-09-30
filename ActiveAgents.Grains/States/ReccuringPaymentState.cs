using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.States;

[GenerateSerializer]
[Alias("ActiveAgents.Grains.States.ReccuringPaymentState")]
public record ReccuringPaymentState
{
    [Id(0)]
    public Guid Id { get; set; }

    [Id(1)]
    public decimal Ammount { get; set; }

    [Id(2)]
    public int FrequencyInSeconds { get; set; }
}