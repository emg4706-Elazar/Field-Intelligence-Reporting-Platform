using Consumer.Mappers;
using Consumer.Models;
using System;
using System.Text.Json;


namespace Consumer.Services;

public class ReportProcessorService
{
    private readonly ReportValidationService _validator;
    private readonly ElasticsearchReportService
        _elasticsearchService;

    public ReportProcessorService(
        ReportValidationService validator,
        ElasticsearchReportService elasticsearchService)
    {
        _validator = validator;
        _elasticsearchService = elasticsearchService;
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
            Console.WriteLine(
                $"Rejected: Invalid JSON. {ex.Message}");

            return ProcessingResult.Handled;
        }

        // Whether returned null
        if (report is null)
        {
            Console.WriteLine(
                "Rejected: Message contains no report.");

            return ProcessingResult.Handled;
        }

        // Validate the fields values
        ValidationResult validationResult =
            _validator.Validate(report);

        if (!validationResult.IsValid)
        {
            Console.WriteLine($"Report rejected: " +
                $"{validationResult.ErrorMessage}");

            return ProcessingResult.Handled;
        }

        // Convert into StoredReport
        StoredReport storedReport =
            ReportMapper.ToStoredReport(report);

        Console.WriteLine(
            $"Valid Report: {storedReport.ReportId}");

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
