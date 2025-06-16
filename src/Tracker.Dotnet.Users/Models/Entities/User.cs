using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable
namespace Tracker.Dotnet.Users.Models.Entities;

[Table("Users")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string Login { get; set; }

    [Required]
    public string DisplayName { get; set; }

    public bool IsActive { get; set; }

    [ForeignKey(nameof(Role))]
    [Required]
    public string RoleName { get; set; }

    public Role Role { get; set; }
}
