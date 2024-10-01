using ActiveAgents.Grains.Filters;
using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveAgents.Silo;

public class Program
{
    public const string ClusterName = "Gorun-XYZ-Cluster";
    public const string ClusterId = "XYZ-762851DA0068AL";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Silo is starting up...");

        // Create a CancellationTokenSource
        using var cancellationTokenSource = new CancellationTokenSource();

        // Optionally listen for CTRL+C or SIGTERM for graceful shutdown
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
            Console.WriteLine("Cancellation requested...");
        };

        await Host.CreateDefaultBuilder(args)
             .UseOrleans(siloBuilder =>
               {
                   siloBuilder.UseAzureStorageClustering(configureOptions: options =>
                   {
                       options.TableServiceClient = new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true;");
                   });

                   siloBuilder.Configure<ClusterOptions>(options =>
                   {
                       options.ClusterId = ClusterName;
                       options.ServiceId = ClusterId;
                   });

                   siloBuilder.AddAzureTableGrainStorage("tableStorage", configureOptions: options =>
                   {
                       options.TableServiceClient = new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true;");
                   });
                   siloBuilder.AddAzureBlobGrainStorage("blobStorage", configureOptions: options =>
                   {
                       options.BlobServiceClient = new Azure.Storage.Blobs.BlobServiceClient("UseDevelopmentStorage=true;");
                   });
                   siloBuilder.AddAzureTableGrainStorageAsDefault(configureOptions: options =>
                   {
                       options.TableServiceClient = new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true;");
                   });

                   siloBuilder.UseAzureTableReminderService(configureOptions: options =>
                   {
                       options.Configure(o => o.TableServiceClient = new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true;"));
                   });

                   siloBuilder.AddAzureTableTransactionalStateStorageAsDefault(options =>
                   {
                       options.Configure(o => o.TableServiceClient = new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true;"));
                   });

                   siloBuilder.UseTransactions();

                   siloBuilder.AddAzureQueueStreams("QueueStreamProvider", options =>
                   {
                       options.Configure(o =>
                       {
                           o.QueueServiceClient = new QueueServiceClient("UseDevelopmentStorage=true;");
                       });

                       //options.ConfigurePullingAgent(c => c.Configure(s => s.GetQueueMsgsTimerPeriod = TimeSpan.FromSeconds(10)));

                   }).AddAzureTableGrainStorage("PubSubStore", options =>
                   {
                       options.Configure(o => o.TableServiceClient = new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true;"));
                   });

                   siloBuilder.AddIncomingGrainCallFilter<LoggingIncomingGrainCallFilter>();
                   siloBuilder.AddOutgoingGrainCallFilter<LoggingOutgoingGrainCallFilter>();
                   //siloBuilder.Configure<GrainCollectionOptions>(options =>
                   //{
                   //    options.CollectionQuantum = TimeSpan.FromSeconds(20);
                   //    options.CollectionAge = TimeSpan.FromSeconds(20);
                   //});
               }).RunConsoleAsync(cancellationTokenSource.Token);

        Console.WriteLine("Silo Started...");
    }
}