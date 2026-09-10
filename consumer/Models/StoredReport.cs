using System.Text.Json.Serialization;

namespace Consumer.Models;

public class StoredReport
{
    public string ReportId { get; set; } = null!;

    [JsonPropertyName("@timestamp")]
    public DateTimeOffset Timestamp { get; set; }
    public string AgentId { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public string Theater { get; set; } = null!;
    public string Sector { get; set; } = null!;
    public string Location { get; set; } = null!;
    public string ReportType { get; set; } = null!;
    public string Priority { get; set; } = null!;
    public string SourceType { get; set; } = null!;
    public string Message { get; set; } = null!;
    public DateTimeOffset ProcessedAt { get; init; }

    public string? SubjectId { get; set; }
    public string? SubjectType { get; set; }
}


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportType
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
public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}
