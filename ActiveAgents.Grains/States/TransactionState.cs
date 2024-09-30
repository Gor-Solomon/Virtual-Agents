using Orleans;
using System;
using System.Collections.Generic;

namespace ActiveAgents.Grains.States;

[GenerateSerializer]
[Alias("ActiveAgents.Grains.States.TransactionState")]
public record TransactionState
{
    [Id(0)]
    public Guid From { get; set; }

    [Id(1)]
    public Guid To { get; set; }

    [Id(2)]
    public decimal Amount { get; set; }

    [Id(3)]
    public DateTime Timestamp { get; set; }

    [Id(4)]
    public string Purpose { get; set; }
}
