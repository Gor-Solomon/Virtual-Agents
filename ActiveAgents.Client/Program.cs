using ActiveAgents.Client.Contracts;
using ActiveAgents.Grains.Abstraction;
using ActiveAgents.Grains.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveAgents.Client;

public class Program
{
    public const string ClusterName = "Gorun-XYZ-Cluster";
    public const string ClusterId = "XYZ-762851DA0068AL";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.UseOrleansClient((client) =>
        {
            client.UseAzureStorageClustering(configureOptions: options =>
            {
                options.TableServiceClient = new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true;");
            });
            client.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = ClusterName;
                options.ServiceId = ClusterId;
            });
            client.UseTransactions();
        });

        var app = builder.Build();

        Accounts(app);
        ATM(app);
        Customers(app);

        // Azurite
        app.Run();
    }


    private static void Accounts(WebApplication app)
    {
        app.MapPost("accounts", async (CreateContract createContract, IClusterClient cluster, ITransactionClient transactionClient) =>
        {
            var grainId = Guid.NewGuid();

            await transactionClient.RunTransaction(TransactionOption.Create, async () =>
            {
                var grain = cluster.GetGrain<IAccountGrain>(grainId);
                await grain.Initialize(createContract.OppeningBalance);
            });

            return TypedResults.Ok(grainId);
        });

        app.MapGet("accounts/{grainId:guid}", async (Guid grainId, IClusterClient cluster, ITransactionClient transactionClient) =>
        {
            decimal? result = null;

            await transactionClient.RunTransaction(TransactionOption.Create, async () =>
            {
                var grain = cluster.GetGrain<IAccountGrain>(grainId);
                result = await grain.GetBalance();
            });

            return TypedResults.Ok(result);
        });

        app.MapPost("accounts/{grainId:guid}/debit", async ([FromRoute, Required] Guid grainId, [FromQuery, Required] decimal amount, IClusterClient cluster, ITransactionClient transactionClient) =>
        {
            await transactionClient.RunTransaction(TransactionOption.Create, async () =>
            {
                var grain = cluster.GetGrain<IAccountGrain>(grainId);
                await grain.Debit(amount);
            });

            return TypedResults.Ok();
        });

        app.MapPost("accounts/{grainId:guid}/credit", async ([FromRoute, Required] Guid grainId, [FromQuery, Required] decimal amount, IClusterClient cluster, ITransactionClient transactionClient) =>
        {
            await transactionClient.RunTransaction(TransactionOption.Create, async () =>
            {
                var grain = cluster.GetGrain<IAccountGrain>(grainId);
                await grain.Credit(amount);
            });

            return TypedResults.Ok();
        });

        app.MapPost("accounts/{grainId:guid}/recuringPayment", async ([FromRoute, Required] Guid grainId, [FromQuery, Required] decimal amount, [FromQuery, Required] int frequencyInSeconds, IClusterClient cluster) =>
        {
            var grain = cluster.GetGrain<IAccountGrain>(grainId);
            await grain.AddReccuringPayment(grainId, amount, frequencyInSeconds);

            return TypedResults.Ok();
        });

        app.MapPost("accounts/{grainId:guid}/FireAndForget", async ([FromRoute, Required] Guid grainId, IClusterClient cluster) =>
        {
            var grain = cluster.GetGrain<IAccountGrain>(grainId);
            await grain.FireAndForget();

            return TypedResults.Ok();
        });

        app.MapPost("accounts/{grainId:guid}/CancelableWork", async ([FromRoute, Required] Guid grainId, IClusterClient cluster, CancellationToken cancellationToken) =>
        {
            var grain = cluster.GetGrain<IAccountGrain>(grainId);
            
            var grainCancelationTokenSource = new GrainCancellationTokenSource();
            cancellationToken.Register(() => grainCancelationTokenSource.Cancel());

            await grain.CancelableWork(5, grainCancelationTokenSource.Token);

            return TypedResults.Ok();
        });

        app.MapPost("accounts/transfer", async ([FromQuery, Required] Guid fromAccountId, [FromQuery, Required] Guid toAccountId, [FromQuery, Required] decimal amount, IClusterClient cluster) =>
        {
            var grain = cluster.GetGrain<ITransferProcessingStatlessGrain>(0);
            var transactions =  await grain.Transfer(fromAccountId, toAccountId, amount);

            return TypedResults.Ok(transactions.ToList());
        });
    }

    private static void ATM(WebApplication app)
    {
        app.MapPost("atm", async ([FromQuery, Required] decimal amount, IClusterClient cluster, ITransactionClient transactionClient) =>
        {
            var grainId = Guid.NewGuid();

            await transactionClient.RunTransaction(TransactionOption.Create, async () =>
            {
                var grain = cluster.GetGrain<IAtmGrain>(grainId);
                await grain.Initialise(amount);
            });

            return TypedResults.Ok(grainId);
        });

        app.MapGet("atm/{atmId:guid}", async ([FromRoute, Required] Guid atmId, IClusterClient cluster, ITransactionClient transactionClient) =>
        {
            decimal cashe = 0;

            await transactionClient.RunTransaction(TransactionOption.Create, async () =>
            {
                var grain = cluster.GetGrain<IAtmGrain>(atmId);
                cashe = await grain.CheckCashe();
            });

            return TypedResults.Ok(cashe);
        });

        app.MapPost("atm/{atmId:guid}/withdraw", async ([FromRoute, Required] Guid atmId, [FromQuery, Required] Guid accountId, [FromQuery, Required] decimal amount, IClusterClient cluster, ITransactionClient transactionClient) =>
        {
            await transactionClient.RunTransaction(TransactionOption.Create, async () =>
            {
                var atmGrain = cluster.GetGrain<IAtmGrain>(atmId);
                await atmGrain.Withdraw(accountId, amount);

                var accountGrain = cluster.GetGrain<IAccountGrain>(accountId);
                await accountGrain.Debit(amount);
            });

            return TypedResults.Ok();
        });
    }

    private static void Customers(WebApplication app)
    {
        app.MapPost("customer/", async ([FromQuery, Required] string fullName, IClusterClient cluster) =>
        {
            var grainId = Guid.NewGuid();

            var grain = cluster.GetGrain<ICustomerGrain>(grainId);
            await grain.Initialize(fullName);

            return TypedResults.Ok(grainId);
        });

        app.MapGet("customer/{customerId:guid}/networth", async ([FromRoute, Required] Guid customerId, IClusterClient cluster) =>
        {
            var grain = cluster.GetGrain<ICustomerGrain>(customerId);
            
            var netWorth = await grain.GetNetWorth();

            return TypedResults.Ok(new { Name = netWorth.name, Balance = netWorth.netWorth, Accounts = netWorth.accounts });
        });

        app.MapPost("customer/{customerId:guid}/account", async ([FromRoute, Required] Guid customerId, [FromQuery, Required] decimal OpeningBalance, IClusterClient cluster) =>
        {
            var grain = cluster.GetGrain<ICustomerGrain>(customerId);
            
            var accountId = await grain.CreateAccount(OpeningBalance);
            
            return TypedResults.Ok(accountId);
        });
    }
}