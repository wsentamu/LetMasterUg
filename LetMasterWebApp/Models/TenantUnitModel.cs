using LetMasterWebApp.Core;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetMasterWebApp.Models;

public class Tenant : BaseModel
{
    [Column(TypeName = "varchar(200)")]
    public string Name { get; set; } = default!;
    [Column(TypeName = "varchar(25)")]
    public string? PhoneNumber { get; set; }
    [Column(TypeName = "varchar(25)")]
    public string? MobileNumber { get; set; }
    [Column(TypeName = "varchar(100)")]
    public string? Email { get; set; }
    public string? UserId { get; set; }
}
public class TenantUnit : BaseModel
{
    public int UnitId { get; set; }
    public int TenantId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Now;
    [Column(TypeName = "decimal(15, 2)")]
    public decimal? AgreedRate { get; set; }
    [Column(TypeName = "decimal(15, 2)")]
    public decimal? CurrentBalance { get; set; } = 0;
}