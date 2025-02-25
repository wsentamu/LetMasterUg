using LetMasterWebApp.Core;

namespace LetMasterWebApp.Models;
public class UserStats
{
    public int UserCount { get; set; }
    public List<UserRolesCount> RolesCount { get; set; } = new List<UserRolesCount>();
}
public class UserRolesCount
{
    public string? RoleLabel { get; set; }
    public int? RoleCount { get; set; }
}
public class TenantUnitTransactionView
{
    public string? PropertyName { get; set; }
    public string? UnitNo { get; set; }
    public string? TransactionType { get; set; }
    public decimal Amount { get; set; } = 0.0m;
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; }
    public int? TenantUnitId { get; set; }
    public string? TenantName { get; set; }
    public DateTime? DatePosted { get; set; }
}
public class PropertyOccupancy
{
    public int? TotalProperties { get; set; }
    public int? TotalUnits { get; set; }
    public int? TotalOccupied { get; set; }
    public decimal? OccupancyRate { get; set; }
}
public class PerPropertyOccupancy
{
    public string? PropertiesName { get; set; }
    public int? TotalUnits { get; set; }
    public int? TotalOccupied { get; set; }
    public decimal? OccupancyRate { get; set; }
}
public class Debtor
{
    public decimal Amount { get; set; } = 0.0m;
    public string? PropertyName { get; set; }
    public string? UnitNo {  get; set; }
    public string? TenantName { get; set; }
    public decimal? AgreedRate { get; set; } = 0.0m;
    public DateTime? DateLastPaid { get; set; }
    public decimal? AmountLastPaid { get; set; } = 0.0m;
}
public class IncomeExpense
{
    public int PropertyId { get; set; }
    public string? PropertyName { get; set; }
    public decimal TotalExpenses { get; set; } = 0.0m;
    public decimal TotalIncomeRecieved { get; set; } = 0.0m;
    public decimal TotalRentRecievable { get; set; } = 0.0m;
}
public class TenantAccount
{
    public int Id { get; set; }
    public string? PropertyName { get; set; }
    public string? PropertyManager { get; set; }
    public string? UnitName { get; set; }
    public decimal? AgreedRate { get; set; } = 0.0m;
    public decimal? CurrentBalance { get; set; } = 0.0m;
}
public class PropertyRentTransactionEntry
{
    public int? UnitId { get; set; }
    public int? AccountId { get; set; }
    public int? TransactionId { get; set; }
    public string? UnitNo { get; set; }
    public string? TenantName {  get; set; }
    public string? TransactionDesc { get; set;}
    public string? TransactionType { get; set;}
    public decimal? Amount { get; set; } = 0.0m;
    public DateTime? TransactionDate { get; set; }
    public DateTime? TransactionCreationDate { get; set; }
    public string? TransactionMode {  get; set; }
    public string? TransactionRef {  get; set; }
}
public class PropertyRentTransaction
{
    public int PropertyId { get; set; }
    public string? PropertyName { get; set; }
    public string? PropertyAddress { get; set; }
    public List<PropertyRentTransactionEntry>? TransactionEntries { get; set; }
}
public class PropertyExpenseEntry
{
    public int? UnitId { get; set; }
    public string? UnitNo { get; set; }
    public string? ExpenseDesc { get; set; }
    public string? ExpenseType { get; set; }
    public int? ExpenseId { get; set; }
    public decimal? Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime? TransactionCreationDate { get; set; }
}
public class PropertyExpense
{
    public int PropertyId { get; set; }
    public string? PropertyName { get; set; }
    public string? PropertyAddress { get; set; }
    public List<PropertyExpenseEntry>? Expenses { get; set; }
}
public class UnitAccountTransaction //for manager get all transactions related to unit for tenant get account transactions
{
    public int TransactionId { get; set; }
    public string? TransactionDesc { get; set; }
    public string? TransactionType { get; set; }
    public decimal? Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime? TransactionCreationDate { get; set; }
    public string? TransactionMode { get; set; }
    public string? TransactionRef { get; set; }
}
public class AccountStatement
{
    public int? AccountId { get; set; }
    public string? Tenant { get; set; }
    public string? TenantContact { get; set; }
    public string? TenantEmail { get; set; }
    public string? PropertyName { get; set; }
    public string? PropertyAddress { get; set; }
    public string? PropertyManager { get; set; }
    public int UnitId { get; set; }
    public string? UnitNo { get; set; }
    public string? ContactNumber { get; set; }
    public string? ContactEmail { get; set; }
    public decimal? AgreedRate { get; set; } = 0.0m;
    public decimal? CurrentBalance { get; set; } = 0.0m;
    public List<UnitAccountTransaction>? AccountTransactions { get; set; }
}
public class UnitDetailsViewModel
{
    public string? UnitNo { get; set; }
    public string? UnitType { get; set; }
    public string? UnitDesc { get; set; }
    public bool IsOccupied { get; set; }
    public string? TenantName { get; set; }
    public string? TenantContact { get; set; }
    public string? TenantEmail { get; set; }
    public decimal? StandardRate { get; set; } = 0.0m;
    public decimal? AgreedRate { get; set; } = 0.0m;
    public decimal? CurrentBalance { get; set; } = 0.0m;
    public DateTime? StartDate { get; set; }
}
public class PropertyOccupancyReport
{
    public string? PropertyName { get; set; }
    public string? PropertyType { get; set; }
    public string? PropertyAddress { get; set; }
    public string? PropertyDesc {  get; set; }
    public int TotalUnits { get; set; } = 0;
    public int OccupiedUnits { get; set; } = 0;
    public List<UnitDetailsViewModel>? UnitDetails { get; set; }
}
public class ReportSearchModel:BaseSearch
{
    public string? UserId { get; set; }
    public int? PropertyId { get; set; }
    public int? TenantAccountId { get; set; }
    public string? TransactionType { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? Subject { get; set; }
    public DateTime DateFrom { get; set; } = DateTime.Now.AddMonths(-6);
    public DateTime DateTo { get; set; } = DateTime.Now;
}
