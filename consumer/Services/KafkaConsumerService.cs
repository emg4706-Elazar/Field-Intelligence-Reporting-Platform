using Confluent.Kafka;
using Consumer.Models;
using Elastic.Clients.Elasticsearch.Tasks;
using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Consumer.Services;

public class KafkaConsumerService
{
    private readonly ReportProcessorService
        _reportProcessor;
    private readonly string _bootstrapServers;
    private readonly string _topicName;
    private readonly string _groupId;
    private readonly ILogger<
        KafkaConsumerService> _logger;

    public KafkaConsumerService(
        ReportProcessorService
        reportProcessor,
        string bootstrapServers,
        string topicName,
        string groupId,
        ILogger<
        KafkaConsumerService> logger
        )
    {
        _reportProcessor = reportProcessor;
        _bootstrapServers = bootstrapServers;
        _topicName = topicName;
        _groupId = groupId;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        // consumer configuration
        var consumerConfiguration = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        // Create the consumer
        using var consumer =
            new ConsumerBuilder<Ignore, string>(
                consumerConfiguration).Build();

        // follow after this topic
        consumer.Subscribe(_topicName);

        _logger.LogInformation(
            "Listening to topic {TopicName}.",
            _topicName);


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
                    await _reportProcessor.ProcessAsync(
                        jsonMessage);

                if (processingResult == ProcessingResult.Retry)
                {
                    _logger.LogWarning(
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
            _logger.LogInformation("Consumer closed.");
        }
    }
}

