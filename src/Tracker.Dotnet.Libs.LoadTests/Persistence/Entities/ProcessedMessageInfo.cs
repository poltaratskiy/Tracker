using System.ComponentModel.DataAnnotations;

namespace Tracker.Dotnet.Libs.LoadTests.Persistence.Entities;

public class ProcessedMessageInfo
{
    [Key]
    public required string MessageId { get; set; }
    public Guid InstanceId { get; set; }
    public required string Content { get; set; }
    public DateTime DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    public TimeSpan? Duration { get; set; }
}
