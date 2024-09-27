using System.Runtime.Serialization;

namespace ActiveAgents.Client.Contracts;

[DataContract]
public class CreateContract
{
    [DataMember]
    public decimal OppeningBalance { get; set; }
}