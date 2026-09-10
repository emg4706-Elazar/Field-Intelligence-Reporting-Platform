using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Consumer.Services;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;

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

        // Register the logger
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConfiguration(
                configuration.GetSection("Logging"));
            logging.AddConsole();
        });

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
                    var logger =
                        serviceProvider
                            .GetRequiredService<ILogger<
                                ElasticsearchReportService>>();

                    return new ElasticsearchReportService(
                        client,
                        indexName,
                        logger);
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

                var logger =
                    serviceProvider.GetRequiredService<
                        ILogger<KafkaConsumerService>>();

                return new KafkaConsumerService(
                    reportProcessor,
                    bootstrapServers,
                    topicName,
                    groupId,
                    logger);
            });


        // ========== Generate The Services ============
        using var serviceProvider = services
            .BuildServiceProvider();

        var logger =
            serviceProvider
            .GetRequiredService<ILogger<Program>>();

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
            logger.LogCritical(
                "Elasticsearch is unavailable. " +
                "The consumer cannot start.");

            return;
        }

        logger.LogInformation(
            "Connected to Elasticsearch successfully");
        
        // Ensure the index exists
        await elasticsearchService
            .EnsureIndexExistsAsync();

        // ========== Run The Consumer ===========
        await kafkaConsumer.RunAsync();
    }
}