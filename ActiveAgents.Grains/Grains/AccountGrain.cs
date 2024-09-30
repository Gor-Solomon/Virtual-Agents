using ActiveAgents.Grains.Abstraction;
using ActiveAgents.Grains.Events;
using ActiveAgents.Grains.States;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.Transactions.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace ActiveAgents.Grains.Grains;

[Reentrant]
public class AccountGrain : Grain, IAccountGrain, IRemindable
{
    private const string _recuringPaymentReminderPrefix = "RecuringPayment::";
    private readonly ITransactionClient _transactionClient;
    private readonly ITransactionalState<BalanceState> _balanceState;
    private readonly IPersistentState<AccountState> _checkingAccountState;

    public AccountGrain(
        ITransactionClient transactionClient,
        [TransactionalState(nameof(BalanceState), "tableStorage")] ITransactionalState<BalanceState> balanceState,
        [PersistentState(nameof(AccountState), "blobStorage")] IPersistentState<AccountState> checkingAccountState)
    {
        _transactionClient = transactionClient;
        _balanceState = balanceState;
        _checkingAccountState = checkingAccountState;
    }

    public async Task Initialize(decimal opeeningBalance)
    {
        await _balanceState.PerformUpdate(s =>
        {
            s.Balance = opeeningBalance;
        });

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

        await _balanceState.PerformUpdate(s=> s.Balance += amount);

        Console.WriteLine($"Finished Account {this.GetPrimaryKey()} Credit process");

        var streamProvider = this.GetStreamProvider("QueueStreamProvider");

        var streamId = StreamId.Create("BalanceStream", this.GetPrimaryKey());

        var stream = streamProvider.GetStream<BalanceChangeEvent>(streamId);
        var bcEvent = new BalanceChangeEvent() { AccountId = this.GetPrimaryKey(), Balance = await GetBalance() };

        await stream.OnNextAsync(bcEvent);
    }

    public async Task Debit(decimal amount)
    {
        Console.WriteLine($"Starting Account {this.GetPrimaryKey()} Debit process");

        var existingBalance = await _balanceState.PerformRead(s => s.Balance);

        if (existingBalance - amount < 0)
        {
            throw new InvalidOperationException("Rejected, Insufficent Credit...");
        }

        await _balanceState.PerformUpdate(s => s.Balance -= amount);

        Console.WriteLine($"Finished Account {this.GetPrimaryKey()} Debit process");

        var streamProvider = this.GetStreamProvider("QueueStreamProvider");

        var streamId = StreamId.Create("BalanceStream", this.GetPrimaryKey());

        var stream = streamProvider.GetStream<BalanceChangeEvent>(streamId);
        var bcEvent = new BalanceChangeEvent() { AccountId = this.GetPrimaryKey(), Balance = await GetBalance() };

        await stream.OnNextAsync(bcEvent);
    }

    public async Task<decimal> GetBalance() => await _balanceState.PerformRead(s => s.Balance);

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
            var registredGrain = id.Equals(this.GetPrimaryKey()) ? this : GrainFactory.GetGrain<IAccountGrain>(id);

            await _transactionClient.RunTransaction(TransactionOption.Create, async () =>
            {
                await registredGrain.Debit(recuringPayment.Ammount);
            });
        }
    }
}