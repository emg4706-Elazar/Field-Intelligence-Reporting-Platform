using Confluent.Kafka;
using Consumer.Models;
using Microsoft.Extensions.Configuration;

namespace Consumer;

public class Program
{
    static void Main(string[] args)
    {
        //================ Setup Configuration ==============
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(
                "appsettings.json",
                optional: false,
                reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Kafka configuration
        var bootstrapServers = configuration["Broker:BootstrapServers"]
            ?? throw new InvalidOperationException(
                "Failed: 'bootstrapServers' is missing.");

        var topicName = configuration["Broker:Topic"]
            ?? throw new InvalidOperationException(
                "Failed: 'topicName' is missing.");

        var groupId = configuration["Kafka:GroupId"]
            ?? throw new InvalidOperationException(
                "Failed: 'GroupId' is missing.");

        // elaticsearch configuration
        var elasticUrl = configuration["Elasticsearch:Url"]
            ?? throw new InvalidOperationException(
                "Failed: URL to 'elasticsearch' is missing.");

        var indexName = configuration["Elasticsearch:IndexName"]
            ?? throw new InvalidOperationException(
                "Failed: 'indexName' is missing.");


        // ========= Generate consumer ===========
        // consumer configuration
        var consumerConfiguration = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = configuration["Broker:GroupId"],
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

                consumer.Commit(result);

                Console.WriteLine(jsonMessage);
            }
        }
        finally
        {
            consumer.Close();
            Console.WriteLine("Consumer closed.");
        }
    }
}