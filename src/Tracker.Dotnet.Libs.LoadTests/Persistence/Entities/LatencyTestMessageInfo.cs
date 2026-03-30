using System.ComponentModel.DataAnnotations;

namespace Tracker.Dotnet.Libs.LoadTests.Persistence.Entities;

public class LatencyTestMessageInfo
{
    [Key]
    public required string MessageId { get; set; }
    public required string Content { get; set; }
    public DateTime DateSent { get; set; }
    public DateTime? DateReceived { get; set; }
    public TimeSpan? Duration { get; set; }
}
