using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.Dotnet.Auth.Models.Entities
{
    [Table("RefreshToken")]
    public class RefreshToken
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public required string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public required User User { get; set; }

        public required string TokenHash { get; set; }

        public DateTime CreatedAt { get; set; }

        public RefreshTokenStatus Status { get; set; }
    }
}
