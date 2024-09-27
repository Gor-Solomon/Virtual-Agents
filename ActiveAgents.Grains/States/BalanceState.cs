using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.States;

[GenerateSerializer]
public class BalanceState
{
    [Id(0)]
    public decimal Balance { get; set; }
}