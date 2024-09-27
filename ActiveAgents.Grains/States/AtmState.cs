using Orleans;
using System;

namespace ActiveAgents.Grains.States;

[GenerateSerializer]
[Alias("ActiveAgents.Grains.States.AtmState")]
public record AtmState
{
    [Id(0)]
    public Guid Id { get; set; }

    [Id(1)]
    public decimal Balance { get; set; }
}
