using LetMasterWebApp.Core;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetMasterWebApp.Models;
public class Property : BaseModel
{
    public string? PropertyManagerId { get; set; }
    [Column(TypeName = "varchar(200)")]
    public string? Name { get; set; }
    public int? PropertyTypeId { get; set; }
    [Column(TypeName = "varchar(250)")]
    public string? Description { get; set; }
    [Column(TypeName = "varchar(250)")]
    public string? Address { get; set; }
    [Column(TypeName = "varchar(25)")]
    public string? Latitude { get; set; }
    [Column(TypeName = "varchar(25)")]
    public string? Longitude { get; set; }
}
public class Unit : BaseModel
{
    public int PropertyId { get; set; }
    [Column(TypeName = "varchar(200)")]
    public string? Name { get; set; }
    public int? UnitTypeId { get; set; }
    [Column(TypeName = "varchar(250)")]
    public string? Description { get; set; }
    [Column(TypeName = "decimal(15, 2)")]
    public decimal? StandardRate { get; set; } = 0;
    public bool IsOccupied { get; set; } = false;
}
public class MaintenaceRequest : BaseModel
{
    public int? PropertyId { get; set; }
    public int? UnitId { get; set; }
    public string? Description { get; set; }
    public string? CurrentStatus {  get; set; }
}