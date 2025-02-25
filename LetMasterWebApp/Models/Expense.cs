using LetMasterWebApp.Core;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetMasterWebApp.Models;
public class Expense : BaseModel
{
    public int ExpenseTypeId { get; set; }
    public int? PropertyId { get; set; }
    public int? UnitId { get; set; }
    [Column(TypeName = "varchar(250)")]
    public string? Description { get; set; }
    [Column(TypeName = "decimal(15, 2)")]
    public decimal? Amount { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
}
