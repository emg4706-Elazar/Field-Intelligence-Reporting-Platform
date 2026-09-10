using Consumer.Models;

namespace Consumer.Mappers;

public static class ReportMapper
{
    public static StoredReport ToStoredReport(
        IncomingReport incomingReport)
    {
        return new StoredReport
        {
            ReportId = incomingReport.ReportId!,

            Timestamp = DateTimeOffset.Parse(
                incomingReport.Timestamp!),

            AgentId = incomingReport.AgentId!,
            Unit = incomingReport.Unit!,
            Theater = incomingReport.Theater!,
            Sector = incomingReport.Sector!,
            Location = incomingReport.Location!,
            ReportType = incomingReport.ReportType!,
            Priority = incomingReport.Priority!,
            SourceType = incomingReport.Message!,

            SubjectId = incomingReport.SubjectId,
            SubjectType = incomingReport.SubjectType,

            ProcessedAt = DateTime.UtcNow
        };
    }
}
