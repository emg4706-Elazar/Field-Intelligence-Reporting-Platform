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

        // Register ElasticsearchClient
        services.AddSingleton(elasticClient);

        // Register ElasticService
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

        // Register ValidationService
        services.AddSingleton<
            ReportValidationService>();

        // Register ProcessorService
        services.AddSingleton<ReportProcessorService>();

        // Register ConsumerService
        services.AddSingleton<KafkaConsumerService>(
            serviceProvider =>
            {
                var reportProcessor =
                    serviceProvider.GetRequiredService<
                        ReportProcessorService>();

                return new KafkaConsumerService(
                    reportProcessor,
                    bootstrapServers,
                    topicName,
                    groupId);
            });


        // ========== Generate The Services ============
        using var serviceProvider = services
            .BuildServiceProvider();

        var elasticsearchService =
            serviceProvider
            .GetRequiredService<ElasticsearchReportService>();

        var kafkaConsumer =
            serviceProvider
            .GetRequiredService<KafkaConsumerService>();

        // Check availability
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

        // ========== Run The Consumer ===========
        await kafkaConsumer.RunAsync();
    }
}