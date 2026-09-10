using Consumer.Models;
using Elastic.Clients.Elasticsearch;

namespace Consumer.Services;

public class ElasticsearchReportService
{
    private readonly ElasticsearchClient _client;
    private readonly string _indexName;

    public ElasticsearchReportService(
        ElasticsearchClient client,
        string indexName)
    {
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
            throw new InvalidOperationException(
                $"Failed to check whether index " +
                $"'{_indexName}' exists. " +
                $"{existsResponse.DebugInformation}");
        }

        if (existsResponse.Exists)
        {
            Console.WriteLine(
                $"Index '{_indexName}' already exists.");

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
            throw new InvalidOperationException(
                $"Failed to create index " +
                $"'{_indexName}'. " +
                $"{createResponse.DebugInformation}");
        }

        Console.WriteLine(
            $"Index '{_indexName}' created successfully.");
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
                Console.WriteLine(
                    $"Report '{report.ReportId}' saved.");

                return StoreResult.Created;
            }

            if (response.ApiCallDetails.HttpStatusCode == 409)
            {
                Console.WriteLine(
                    $"Duplicate report rejected: " +
                    $"{report.ReportId}");

                return StoreResult.Duplicate;
            }

            Console.WriteLine(
                $"Failed to save report " +
                $"'{report.ReportId}'. " +
                $"{response.DebugInformation}");

            return StoreResult.Failed;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Elasticsearch communication failed: " +
                $"{ex.Message}");

            return StoreResult.Failed;
        }
    }
}
