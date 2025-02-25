using LetMasterWebApp.Core;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetMasterWebApp.Models;
public class PropertyViewModel
{
    public int Id { get; set; }
    public string? PropertyManagerId { get; set; }
    [Display(Name = "Property Manager")]
    public string? PropertyManager { get; set; }
    [Display(Name = "Property Name")]
    public string? Name { get; set; }
    public int? PropertyTypeId { get; set; }
    [Display(Name = "Property Type")]
    public string? PropertyType { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string? Coordinates { get; set; }
    [Display(Name = "Is Active")]
    public bool IsActive { get; set; }
    [Display(Name = "Total Units")]
    public int TotalUnits { get; set; } = 0;
    [Display(Name = "Occupied Units")]
    public int OccupiedUnits { get; set; } = 0;
}
public class PropertyUpdateModel
{
    public int Id { get; set; }
    public string? PropertyManagerId { get; set; }
    [Display(Name = "Property Name")]
    public string? Name { get; set; }
    [Display(Name = "Property Type")]
    public int? PropertyTypeId { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;
    public string? UpdatedBy { get; set; }
}
public class PropertyCreateModel
{
    [Required(ErrorMessage = "Select a property manager/owner")]
    public string? PropertyManagerId { get; set; }
    [Display(Name = "Property Name")]
    [Required]
    public string? Name { get; set; }
    [Display(Name = "Property Type")]
    public int? PropertyTypeId { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
}
public class PropertySearchModel : BaseSearch
{
    public string? PropertyManagerId { get; set; }
    [Display(Name = "Property Name")]
    public string? Name { get; set; }
    [Display(Name = "Property Type")]
    public int? PropertyTypeId { get; set; }
    public bool IsActive { get; set; } = true;
}
public class UnitSearchModel : BaseSearch
{
    public int PropertyId { get; set; }
    public int? UnitTypeId { get; set; }
    public string? Name { get; set; }
    public bool? IsOccupied { get; set; }
    public bool IsActive { get; set; } = true;
}
public class UnitViewModel
{
    public int Id { get; set; }
    public int? PropertyId { get; set; }
    public string? Name { get; set; }
    public int? UnitTypeId { get; set; }
    [Display(Name = "Unit Type")]
    public string? UnitType { get; set; }
    public string? Description { get; set; }
    [Display(Name = "Standard Rate")]
    public decimal? StandardRate { get; set; } = 0;
    [Display(Name = "Is Occupied")]
    public bool IsOccupied { get; set; } = false;
    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;
}
public class UnitCreateModel
{
    public int PropertyId { get; set; }
    public string? Name { get; set; }
    [Display(Name = "Unit Type")]
    public int? UnitTypeId { get; set; }
    public string? Description { get; set; }
    [Display(Name = "Standard Rate")]
    public decimal? StandardRate { get; set; } = 0;
    [Display(Name = "Is Occupied")]
    public bool IsOccupied { get; set; } = false;
    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    [Display(Name = "Add Multiple Units")]
    public bool IsBulkEntry { get; set; }
    [Display(Name = "Number of Units")]
    public int? BulkCount { get; set; }
    [Display(Name = "Unit Name Pattern")]
    public string? UnitSeries { get; set; }
    [Display(Name = "Use Pattern as Prefix")]
    public bool UnitSeriesPrefix { get; set; } = true;
}
public class UnitUpdateModel
{
    public int Id { get; set; }
    public int? PropertyId { get; set; }
    public string? Name { get; set; }
    [Display(Name = "Unit Type")]
    public int? UnitTypeId { get; set; }
    public string? Description { get; set; }
    [Display(Name = "Standard Rate")]
    public decimal? StandardRate { get; set; } = 0;
    [Display(Name = "Is Occupied")]
    public bool IsOccupied { get; set; } = false;
    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;
    public string? UpdatedBy { get; set; }
}
public class ExpenseCreateModel
{
    [Display(Name = "Expense Category")]
    public int ExpenseTypeId { get; set; }
    public int? PropertyId { get; set; }
    [Display(Name = "Unit Number")]
    public int? UnitId { get; set; }
    [Column(TypeName = "varchar(250)")]
    public string? Description { get; set; }
    [Column(TypeName = "decimal(15, 2)")]
    public decimal? Amount { get; set; }
    [Display(Name = "Transaction Date")]
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}
public class ExpenseViewModel
{
    [Display(Name = "Expense Category")]
    public int ExpenseTypeId { get; set; }
    public int? PropertyId { get; set; }
    [Display(Name = "Unit Number")]
    public int? UnitId { get; set; }
    [Column(TypeName = "varchar(250)")]
    public string? Description { get; set; }
    [Column(TypeName = "decimal(15, 2)")]
    public decimal? Amount { get; set; }
    [Display(Name = "Transaction Date")]
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}
public class ExpenseUpdateModel
{
    [Display(Name = "Expense Category")]
    public int ExpenseTypeId { get; set; }
    public int? PropertyId { get; set; }
    [Display(Name = "Unit Number")]
    public int? UnitId { get; set; }
    [Column(TypeName = "varchar(250)")]
    public string? Description { get; set; }
    [Column(TypeName = "decimal(15, 2)")]
    public decimal? Amount { get; set; }
    [Display(Name = "Transaction Date")]
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}