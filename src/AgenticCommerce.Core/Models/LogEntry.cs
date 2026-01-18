using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenticCommerce.Core.Models;

/// <summary>
/// Database entity for application logs.
/// </summary>
[Table("app_logs")]
public class LogEntry
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("level")]
    [MaxLength(20)]
    public string Level { get; set; } = "Information";

    [Column("message")]
    public string? Message { get; set; }

    [Column("exception")]
    public string? Exception { get; set; }

    [Column("source")]
    [MaxLength(255)]
    public string? Source { get; set; }

    [Column("request_path")]
    [MaxLength(500)]
    public string? RequestPath { get; set; }

    [Column("user_id")]
    [MaxLength(100)]
    public string? UserId { get; set; }

    [Column("properties", TypeName = "jsonb")]
    public string? PropertiesJson { get; set; }
}
