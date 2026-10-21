namespace MyDockerProject.Models;

/// <summary>SQL-backed operational record; business data stays out of the Umbraco content tree.</summary>
public sealed class SiteKitOperationalRecord
{
    public required string Id { get; set; }
    public required string ProjectId { get; set; }
    public required string EntityAlias { get; set; }
    public int SchemaVersion { get; set; }
    public required string DataJson { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
