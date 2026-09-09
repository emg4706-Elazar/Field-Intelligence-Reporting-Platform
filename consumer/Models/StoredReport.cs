using System.Text.Json.Serialization;

namespace Consumer.Models;

public class StoredReport
{
    public string? ReportId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? AgentId { get; set; }
    public string? Unit { get; set; }
    public string? Threater { get; set; }
    public string? Sector { get; set; }
    public string? Location { get; set; }
    public string? ReportType { get; set; }
    public string? Priority { get; set; }
    public string? SourceType { get; set; }
    public string? Message { get; set; }

    public string? SubjectId { get; set; }
    public string? SubjectType { get; set; }
}


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportTypes
{
    Observation,
    Movement,
    Meeting,
    Access,
    Communication,
    Logistics,
    Incident
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Priorityes
{
    Low,
    Medium,
    High,
    Critical
}
