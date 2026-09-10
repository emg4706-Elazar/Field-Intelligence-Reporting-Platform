using Consumer.Mappers;
using Consumer.Models;
using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;


namespace Consumer.Services;

public class ReportProcessorService
{
    private readonly ReportValidationService _validator;
    private readonly ElasticsearchReportService
        _elasticsearchService;
    private readonly ILogger<ReportProcessorService> _logger;

    public ReportProcessorService(
        ReportValidationService validator,
        ElasticsearchReportService elasticsearchService,
        ILogger<ReportProcessorService> logger)
    {
        _validator = validator;
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }

    public async Task<ProcessingResult> ProcessAsync(
        string jsonMessage)
    {
        IncomingReport? report;

        // Try To Convert into IncomingReport
        try
        {
            report = JsonSerializer
                .Deserialize<IncomingReport>(
                jsonMessage);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Report rejected because the JSON is invalid.");

            return ProcessingResult.Handled;
        }

        // Whether returned null
        if (report is null)
        {
            _logger.LogWarning(
                "Report rejected because it contains no report.");

            return ProcessingResult.Handled;
        }

        // Validate the fields values
        ValidationResult validationResult =
            _validator.Validate(report);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Report {ReportId} rejected: {ValidationError}",
                report.ReportId,
                validationResult.ErrorMessage);
  
            return ProcessingResult.Handled;
        }

        // Convert into StoredReport
        StoredReport storedReport =
            ReportMapper.ToStoredReport(report);

        _logger.LogInformation(
            "Report {ReportId} passed validation",
            storedReport.ReportId);

        // Added to elasticsearch
        StoreResult storeResult =
            await _elasticsearchService.CreateAsync(
                storedReport);


        return storeResult switch
        {
            StoreResult.Created =>
                ProcessingResult.Handled,

            StoreResult.Duplicate =>
                ProcessingResult.Handled,

            StoreResult.Failed =>
                ProcessingResult.Retry,

            _ =>
                ProcessingResult.Retry
        };
    }
}
