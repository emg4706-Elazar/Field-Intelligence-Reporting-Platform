using Consumer.Models;

namespace Consumer.Services;

public class ReportValidationService
{
    public ValidationResult Validate(
        IncomingReport report)
    {
        if (string.IsNullOrWhiteSpace(report.ReportId))
        {
            return ValidationResult.Failure(
                "ReportId is required.");
        }

        if (string.IsNullOrWhiteSpace(report.Timestamp))
        {
            return ValidationResult.Failure(
                "Timestamp is required.");
        }

        if (!DateTimeOffset.TryParse(
            report.Timestamp,
            out _))
        {
            return ValidationResult.Failure(
                $"Invalid timestamp: {report.Timestamp}");
        }

        if (string.IsNullOrWhiteSpace(report.AgentId))
        {
            return ValidationResult.Failure(
                "AgentId is required.");
        }

        if (string.IsNullOrWhiteSpace(report.Unit))
        {
            return ValidationResult.Failure(
                "Unit is required.");
        }

        if (string.IsNullOrWhiteSpace(report.Theater))
        {
            return ValidationResult.Failure(
                "Theater is required.");
        }

        if (string.IsNullOrWhiteSpace(report.Sector))
        {
            return ValidationResult.Failure(
                "Sector is required.");
        }

        if (string.IsNullOrWhiteSpace(report.Location))
        {
            return ValidationResult.Failure(
                "Location is required.");
        }

        if (string.IsNullOrWhiteSpace(report.ReportType))
        {
            return ValidationResult.Failure(
                "ReportType is required.");
        }

        if (!Enum.GetNames<ReportType>()
            .Contains(report.ReportType))
        {
            return ValidationResult.Failure(
                $"Invalid ReportType: {report.ReportType}");
        }

        if (string.IsNullOrWhiteSpace(report.SourceType))
        {
            return ValidationResult.Failure(
                "SourceType is required.");
        }

        if (string.IsNullOrWhiteSpace(report.Priority))
        {
            return ValidationResult.Failure(
                "Priority is required.");
        }

        if (!Enum.GetNames<Priority>()
            .Contains(report.Priority))
        {
            return ValidationResult.Failure(
                $"Invalid Priority: {report.Priority}");
        }

        if (string.IsNullOrWhiteSpace(report.Message))
        {
            return ValidationResult.Failure(
                "Message is required.");
        }

        bool hasSubjectId =
            !string.IsNullOrWhiteSpace(report.SubjectId);

        bool hasSubjectType =
            !string.IsNullOrWhiteSpace(report.SubjectType);

        if (hasSubjectId != hasSubjectType)
        {
            return ValidationResult.Failure(
                "SubjectId and SubjectType must appear together.");
        }

        return ValidationResult.Success();
    }
}
