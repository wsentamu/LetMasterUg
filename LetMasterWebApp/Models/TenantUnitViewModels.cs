using LetMasterWebApp.Core;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetMasterWebApp.Models;
public class TenantUnitViewModel
{
    public int TenantUnitId { get; set; }
    public int TenantId { get; set; }
    public string? TenantName { get; set; }
    public string? TenantMobile { get; set; }
    public string? PropertyName { get; set; }
    public int UnitId { get; set; }
    public string? UnitName { get; set; }
    public DateTime StartDate { get; set; }
    public decimal AgreedRate { get; set; } = 0.0m;
    public decimal CurrentBalance { get; set; } = 0.0m;
    public int PropertyId { get; set; }
    public string? PropertyManagerId { get; set; }
    public string? TenantUserId { get; set; }
}
public class TenantUnitSearchModel : BaseSearch
{
    public bool IsActive {  get; set; }=true;
    public string? TenantName { get; set; }
    public int? TenantId { get; set; }
    public string? UnitName { get; set; }
    public int? PropertyId { get; set; }
    public string? PropertyManagerId { get; set; } = default!;
    public string? TenantUserId { get; set; }
}
public class TenantUnitCreateModel
{
    public bool IsExistingTenant { get; set; } = false;
    public bool CreateAccount { get; set; } = false;
    [Display(Name = "Username")]
    public string? UserName { get; set; }
    public string? UserId { get; set; }
    public bool IsIndividual { get; set; } = true;
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }
    [Display(Name = "Surname")]
    public string? LastName { get; set; }
    [Display(Name = "Organization Name")]
    public string? OtherNames { get; set; }
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }
    [Display(Name = "Mobile Number")]
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    [Display(Name = "Property")]
    public int PropertyId { get; set; }
    [Display(Name = "Unit Number")]
    public int UnitId {  get; set; }
    [Display(Name = "Tenant")]
    public int TenantId { get; set; }
    [Display(Name = "Agreed Rate")]
    public decimal AgreedRate {  get; set; }
    [Display(Name = "Amount Deposited")]
    public decimal DepositAmount { get; set; } = 0;
    [Display(Name = "Transaction Mode")]
    public string? TransactionMode { get; set; }
    [Display(Name = "Transaction Ref")]
    public string? TransactionRef { get; set; }
    [Display(Name = "Billing Start Date")]
    public DateTime StartDate { get; set; }= DateTime.Now;
    public string? CreatedBy {  get; set; }
}
public class TenantUnitViewDetail
{
    public int Id { get; set; }
    public decimal? AgreedRate { get; set; } = 0.0m;
    public DateTime StartDate { get; set; }
    public decimal? CurrentBalance { get; set; } = 0.0m;
    public string? TenantName { get; set; }
    public string? TenantMobile { get; set; }
    public string? TenantPhone { get; set; }
    public string? TenantEmail { get; set; }
    public string? UnitNo { get; set; }
    public string? UnitDesc { get; set; }
    public string? UnitType { get; set; }
    public string? PropertyName { get; set; }
    public string? PropertyAddress { get; set; }
    public string? PropertyManagerId { get; set; }
    public string? PropertyManager { get; set; }
    public string? PropertyManagerEmail { get; set; }
    public string? PropertyManagerMobile { get; set; }
    public string? PropertyManagerPhone { get; set; }
    public decimal TotalBills { get; set; } = 0.0m;
    public decimal TotalPayments { get; set; } = 0.0m;
}
public class TenantUnitTransactionViewModel
{
    public string? TransactionType { get; set; }
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public int? TenantUnitId { get; set; }
    public string? TransactionMode { get; set; }
    public string? TransactionRef { get; set; }
}
public class TenantUnitTransactionSearchModel:BaseSearch
{
    public int TenantUnitId { get; set; }
    public DateTime FromDate { get; set; } = DateTime.Now.AddYears(-1);
    public DateTime ToDate { get; set; } = DateTime.Now;
    public bool IsActive {  get; set; }=true;
}
public class TenantUnitTransactionCreateModel
{
    public required string TransactionType { get; set; }
    public required string Description { get; set; }
    public required decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public int TenantUnitId { get; set; }
    public string? TransactionMode { get; set; }
    public string? TransactionRef { get; set; }
    public string? CreatedBy { get; set; }
}
public class MobileMoneyCreateModel
{
    [Display(Name = "Amount To Pay (UGX 500 - 5,000,000)")]
    [Range(500, 5000000, ErrorMessage = "The amount must be between 500 and 5,000,000.")]
    public required decimal Amount { get; set; }
    [RegularExpression(@"^\d{9}$", ErrorMessage = "Phone number must be exactly 9 digits.")]
    [Display(Name = "Airtel Money Number")]
    public required string DebitNumber { get; set; }
    public int TenantUnitId { get; set; }
    public string? CreatedBy { get; set; }
}
public class TenantUnitUpdateModel
{
    public required int Id { get; set; }
    [Display(Name = "Agreed Rate")]
    public decimal? AgreedRate { get; set; }
    [Display(Name = "Account Is Active")]
    public bool IsActive { get; set; } = true;
    public string? UpdatedBy {  get; set; }
}