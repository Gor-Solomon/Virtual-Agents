using Orleans;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ActiveAgents.Grains.States;

[GenerateSerializer]
[Alias("ActiveAgents.Grains.States.CheckingAccountState")]
public record CheckingAccountState
{
    [Id(0)]
    public Guid AccountId { get; set; }

    [Id(1)]
    public DateTime OpenedAtUtc { get; set; }

    [Id(2)]
    public string AccountType { get; set; }

    [Id(3)]
    public ICollection<ReccuringPaymentState> RecuringPayments { get; set; } = new List<ReccuringPaymentState>();
}