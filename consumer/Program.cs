using Confluent.Kafka;
using Consumer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Consumer.Services;
using Elastic.Clients.Elasticsearch;

namespace Consumer;

public class Program
{
    static async Task Main(string[] args)
    {
        //================ Setup Configuration ==============
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(
                "appsettings.json",
                optional: false,
                reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        // Kafka configuration
        var bootstrapServers = configuration["Broker:BootstrapServers"]
            ?? throw new InvalidOperationException(
                "Failed: 'bootstrapServers' is missing.");

        var topicName = configuration["Broker:Topic"]
            ?? throw new InvalidOperationException(
                "Failed: 'topicName' is missing.");

        var groupId = configuration["Broker:GroupId"]
            ?? throw new InvalidOperationException(
                "Failed: 'GroupId' is missing.");

        // Elaticsearch configuration
        var elasticUrl = configuration["Elasticsearch:Url"]
            ?? throw new InvalidOperationException(
                "Failed: URL to 'elasticsearch' is missing.");

        var indexName = configuration["Elasticsearch:IndexName"]
            ?? throw new InvalidOperationException(
                "Failed: 'indexName' is missing.");

        var elasticSettings =
            new ElasticsearchClientSettings(
                new Uri(elasticUrl));

        // Create the elastic connection
        var elasticClient =
            new ElasticsearchClient(elasticSettings);

        // ========== Inject The Dependencies ========
        var services = new ServiceCollection();

        services.AddSingleton(elasticClient);

        services.AddSingleton<
            ElasticsearchReportService>(
                serviceProvider =>
                {
                    var client =
                        serviceProvider
                            .GetRequiredService<
                                ElasticsearchClient>();

                    return new ElasticsearchReportService(
                        client,
                        indexName);
                });

        services.AddSingleton<
            ReportValidationService>();

        services.AddSingleton<ReportProcessorService>();

        // Generate the services
        using var serviceProvider = services
            .BuildServiceProvider();

        var reportProcessor =
            serviceProvider
            .GetRequiredService<ReportProcessorService>();

        var elasticsearchService =
            serviceProvider
            .GetRequiredService<ElasticsearchReportService>();

        // Check availablity
        bool elasticsearchAvailable =
            await elasticsearchService.IsAvailableAsync();

        if (!elasticsearchAvailable)
        {
            throw new InvalidOperationException(
                "Elasticsearch is unavailable.");
        }

        Console.WriteLine(
            "Connected to Elasticsearch successfully.");

        // Ensure the index exists
        await elasticsearchService
            .EnsureIndexExistsAsync();


        // ========= Generate consumer ===========
        // consumer configuration
        var consumerConfiguration = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        // Create the consumer
        using var consumer =
            new ConsumerBuilder<Ignore, string>(
                consumerConfiguration).Build();

        // follow after this topic
        consumer.Subscribe(topicName);

        Console.WriteLine(
            $"Listening to topic {topicName}.");


        // ============ Consume Loop ==========
        try
        {
            while (true)
            {
                var result =
                    consumer.Consume(
                        TimeSpan.FromSeconds(1));

                if (result?.Message?.Value is null)
                    continue;

                string jsonMessage = result.Message.Value;

                ProcessingResult processingResult =
                    await reportProcessor.ProcessAsync(
                        jsonMessage);

                if (processingResult == ProcessingResult.Retry)
                {
                    Console.WriteLine(
                    "processing failed temporarily. " +
                    "Consumer will stop without committing");

                    break;
                }

                consumer.Commit(result);
            }
        }
        finally
        {
            consumer.Close();
            Console.WriteLine("Consumer closed.");
        }
    }
}