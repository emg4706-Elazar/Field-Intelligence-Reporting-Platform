using Consumer.Models;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Consumer.Services;

public class ElasticsearchReportService
{
    private readonly ElasticsearchClient _client;
    private readonly string _indexName;
    private readonly ILogger<ElasticsearchReportService> _logger;

    public ElasticsearchReportService(
        ElasticsearchClient client,
        string indexName,
        ILogger<ElasticsearchReportService> logger)
    {
        _logger = logger;
        _client = client;
        _indexName = indexName;
    }

    public async Task<bool> IsAvailableAsync()
    {
        var response =
            await _client.PingAsync();

        return response.IsValidResponse;
    }


    public async Task EnsureIndexExistsAsync()
    {
        var existsResponse =
            await _client.Indices.ExistsAsync(
                _indexName);

        if (!existsResponse.IsValidResponse)
        {
            _logger.LogError(
                "Failed to check whether Elasticsearch index " +
                "{IndexName} exists. {Details}",
                _indexName,
                existsResponse.DebugInformation);

            throw new InvalidOperationException(
                $"Failed to check whether index '{_indexName}' exists.");
        }

        if (existsResponse.Exists)
        {
            _logger.LogInformation(
                "Index '{IndexName}' already exists.",
                _indexName);

            return;
        }

        var createResponse =
            await _client.Indices.CreateAsync<
                StoredReport>(
                _indexName,
                descriptor => descriptor
                    .Mappings(mapping => mapping
                        .Properties(properties => properties
                            .Keyword(report =>
                                report.ReportId)

                            .Date(report =>
                                report.Timestamp)

                            .Keyword(report =>
                                report.AgentId)

                            .Keyword(report =>
                                report.Unit)

                            .Keyword(report =>
                                report.Theater)

                            .Keyword(report =>
                                report.Sector)

                            .Keyword(report =>
                                report.Location)

                            .Keyword(report =>
                                report.ReportType)

                            .Keyword(report =>
                                report.Priority)

                            .Keyword(report =>
                                report.SourceType)

                            .Text(report =>
                                report.Message)

                            .Date(report =>
                                report.ProcessedAt)

                            .Keyword(report =>
                                report.SubjectId)

                            .Keyword(report =>
                                report.SubjectType)
                        )
                    )
            );

        if (!createResponse.IsValidResponse)
        {
            _logger.LogError(
                "Failed to create index '{IndexName}'. " +
                "{Details}",
                _indexName,
                createResponse.DebugInformation);


            throw new InvalidOperationException(
                $"Failed to create index '{_indexName}'.");
        }

        _logger.LogInformation(
            "Elasticsearch index {indexName} created successfully.",
            _indexName);
    }


    public async Task<StoreResult> CreateAsync(
        StoredReport report)
    {
        try
        {
            var response =
                await _client.CreateAsync(
                    report,
                    descriptor => descriptor
                        .Index(_indexName)
                        .Id(report.ReportId));

            if (response.IsValidResponse)
            {
                _logger.LogInformation(
                    "Report {ReportId} saved successfully.",
                    report.ReportId);

                return StoreResult.Created;
            }

            if (response.ApiCallDetails.HttpStatusCode == 409)
            {
                _logger.LogWarning(
                    "Duplicate report {ReportId} was not saved.",
                    report.ReportId);

                return StoreResult.Duplicate;
            }

            _logger.LogError(
                "Failed to save report {ReportId}. " +
                "{Details}",
                report.ReportId,
                response.DebugInformation);

            return StoreResult.Failed;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Elasticsearch communication failed while saving " +
                "report {ReportId} to index {IndexName}.",
                report.ReportId,
                _indexName);

            return StoreResult.Failed;
        }
    }
}
