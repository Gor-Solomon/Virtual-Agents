using ActiveAgents.Grains.Abstraction;
using ActiveAgents.Grains.States;
using Orleans;
using Orleans.Runtime;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ActiveAgents.Grains.Grains;

public class CheckingAccountGrain : Grain, ICheckingAccountGrain, IRemindable
{
    private const string _recuringPaymentReminderPrefix = "RecuringPayment::";
    private readonly IPersistentState<BalanceState> _balanceState;
    private readonly IPersistentState<CheckingAccountState> _checkingAccountState;

    public CheckingAccountGrain(
        [PersistentState(nameof(BalanceState), "tableStorage")] IPersistentState<BalanceState> balanceState,
        [PersistentState(nameof(CheckingAccountState), "blobStorage")] IPersistentState<CheckingAccountState> checkingAccountState)
    {
        _balanceState = balanceState;
        _checkingAccountState = checkingAccountState;
    }

    public async Task Initialize(decimal opeeningBalance)
    {
        _balanceState.State.Balance = opeeningBalance;
        await _balanceState.WriteStateAsync();

        _checkingAccountState.State.OpenedAtUtc = DateTime.UtcNow;
        _checkingAccountState.State.AccountType = "Default";
        _checkingAccountState.State.AccountId = this.GetGrainId().GetGuidKey();
        await _checkingAccountState.WriteStateAsync();
    }

    public async Task Credit(decimal amount)
    {
        //this.RegisterGrainTimer<BalanceState>(async (state) =>
        //{
        //    Console.WriteLine($"Starting Account {this.GetPrimaryKey()} Credit process");
        //    await Task.Delay(TimeSpan.FromSeconds(1));
        //    Console.WriteLine($"Finished Account {this.GetPrimaryKey()} Credit process");

        //}, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(2));

        Console.WriteLine($"Starting Account {this.GetPrimaryKey()} Credit process");

        _balanceState.State.Balance += amount;
        await _balanceState.WriteStateAsync();

        Console.WriteLine($"Finished Account {this.GetPrimaryKey()} Credit process");
    }

    public async Task Debit(decimal amount)
    {
        Console.WriteLine($"Starting Account {this.GetPrimaryKey()} Debit process");

        if (_balanceState.State.Balance - amount < 0)
        {
            throw new InvalidOperationException("Rejected, Insufficent Credit...");
        }

        _balanceState.State.Balance -= amount;
        await _balanceState.WriteStateAsync();

        Console.WriteLine($"Finished Account {this.GetPrimaryKey()} Debit process");
    }

    public async Task<decimal> GetBalance() => await Task.FromResult(_balanceState.State.Balance);

    public async Task AddReccuringPayment(Guid id, decimal amount, int frequncyInSeconds)
    {
        var recuringPayment = new ReccuringPaymentState()
        {
            Id = id,
            Ammount = amount,
            FrequencyInSeconds = frequncyInSeconds
        };

        _checkingAccountState.State.RecuringPayments.Add(recuringPayment);

        await _checkingAccountState.WriteStateAsync();
        
        await this.RegisterOrUpdateReminder($"{_recuringPaymentReminderPrefix}{id}", TimeSpan.FromSeconds(frequncyInSeconds), TimeSpan.FromSeconds(frequncyInSeconds));
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        var sections = reminderName.Split(_recuringPaymentReminderPrefix);
        if (sections is not null && sections.Length > 1)
        {
            var id = Guid.Parse(sections[1]);
            var recuringPayment = _checkingAccountState.State.RecuringPayments.FirstOrDefault(rp => rp.Id == id) ??
                                  throw new InvalidOperationException($"No recuring payment was registred for account {id}");

            // Old school, no need to register the id, the reminder is tied to the specific instanse of the grain and it will be triggred to it only not to another one.
            var registredGrain = id.Equals(this.GetPrimaryKey()) ? this : GrainFactory.GetGrain<ICheckingAccountGrain>(id);
            await registredGrain.Debit(recuringPayment.Ammount);
        }
    }
}