using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable
namespace Tracker.Dotnet.Users.Models.Entities;

[Table("Roles")]
public class Role
{
    [Key]
    [Required]
    public string Name { get; set; }
}
