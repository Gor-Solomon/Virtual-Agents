using ActiveAgents.Client.Contracts;
using ActiveAgents.Grains.Abstraction;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.ComponentModel.DataAnnotations;

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
        });
        var app = builder.Build();

        app.MapPost("checkingAccount", async (CreateContract createContract, IClusterClient cluster) =>
        {
            var grainId = Guid.NewGuid();
            var grain = cluster.GetGrain<ICheckingAccountGrain>(grainId);
            await grain.Initialize(createContract.OppeningBalance);

            return TypedResults.Created($"/checkingAccount/{grainId}");
        });

        app.MapGet("checkingAccount/{grainId:guid}", async (Guid grainId, IClusterClient cluster) =>
        {
            var grain = cluster.GetGrain<ICheckingAccountGrain>(grainId);
            var result = await grain.GetBalance();
            return TypedResults.Ok(result);
        });

        app.MapPost("checkingAccount/{grainId:guid}/debit", async ([FromRoute, Required] Guid grainId, [FromQuery, Required] decimal amount, IClusterClient cluster) =>
        {
            var grain = cluster.GetGrain<ICheckingAccountGrain>(grainId);
            await grain.Debit(amount);
            return TypedResults.Ok();
        });

        app.MapPost("checkingAccount/{grainId:guid}/credit", async ([FromRoute, Required] Guid grainId, [FromQuery, Required] decimal amount, IClusterClient cluster) =>
        {
            var grain = cluster.GetGrain<ICheckingAccountGrain>(grainId);
            await grain.Credit(amount);
            return TypedResults.Ok();
        });

        app.MapPost("checkingAccount/{grainId:guid}/recuringPayment", async ([FromRoute, Required] Guid grainId, [FromQuery, Required] decimal amount, [FromQuery, Required] int frequencyInSeconds, IClusterClient cluster) =>
        {
            var grain = cluster.GetGrain<ICheckingAccountGrain>(grainId);
            await grain.AddReccuringPayment(grainId, amount, frequencyInSeconds);

            return TypedResults.Ok();
        });

        app.MapPost("atm", async ([FromQuery, Required] decimal amount, IClusterClient cluster) =>
        {
            var grainId = Guid.NewGuid();
            var grain = cluster.GetGrain<IAtmGrain>(grainId);
            await grain.Initialise(amount);

            return TypedResults.Created($"/atm/{grainId}");
        });

        app.MapPost("atm/{atmId:guid}/withdraw", async ([FromRoute, Required] Guid atmId, [FromQuery, Required] Guid accountId, [FromQuery, Required] decimal amount, IClusterClient cluster) =>
        {
            var grain = cluster.GetGrain<IAtmGrain>(atmId);
            await grain.Withdraw(accountId, amount);
            return TypedResults.Ok();
        });

        app.Run();
    }
}