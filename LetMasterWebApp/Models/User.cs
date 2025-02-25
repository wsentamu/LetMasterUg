using LetMasterWebApp.Core;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetMasterWebApp.Models;
public class User : IdentityUser
{
    [Column(TypeName = "varchar(100)")]
    public string? FirstName { get; set; }
    [Column(TypeName = "varchar(100)")]
    public string? OtherNames { get; set; }
    [Column(TypeName = "varchar(100)")]
    public string? LastName { get; set; }
    [Column(TypeName = "varchar(250)")]
    public string? DisplayName { get; set; }
    [Column(TypeName = "varchar(250)")]
    public string? Address { get; set; }
    [Column(TypeName = "varchar(25)")]
    public string? MobileNumber { get; set; }
    public bool IsActive { get; set; } = true;
}