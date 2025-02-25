using LetMasterWebApp.Core;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetMasterWebApp.Models;
public class TenantUnitTransaction : BaseModel
{
    [Column(TypeName = "varchar(10)")]
    public string? TransactionType { get; set; }
    [Column(TypeName = "varchar(250)")]
    public string? Description { get; set; }
    [Column(TypeName = "decimal(15, 2)")]
    public decimal? Amount { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public int? TenantUnitId { get; set; }
    public string? TransactionMode { get; set; }
    public string? TransactionRef { get; set; }
}
public class ClientDebitRequest : BaseModel
{
    [Column(TypeName = "varchar(10)")]
    public string? ProviderName { get; set; }
    public int ClientAccountId {  get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNo {  get; set; }
    public string? SvcRequestBody {  get; set; }
    public string? SvcResponseBody { get; set; }
    public string? SvcCallBackBody { get; set; }
    public string? SvcReferenceNo {  get; set; }
    public string? SvcStatus {  get; set; }
    public string? ReconcileStatus { get; set; } = "P"; //P - PENDING, F - FAILED, C - COMPLETE
}