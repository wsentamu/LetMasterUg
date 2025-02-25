using LetMasterWebApp.Models;
using LetMasterWebApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LetMasterWebApp.Pages;

public class IndexModel : PageModel
{
    public UserStats? userStats {  get; set; }
    public List<TenantUnitTransactionView>? transactions { get; set; }
    public PropertyOccupancy? occupancy { get; set; }
    public List<PerPropertyOccupancy>? perPropertyOccupancies { get; set; }
    public List<Debtor>? debtors { get; set; }
    public List<Debtor>? tenantDebts { get; set; }
    public List<IncomeExpense>? incomeExpenses { get; set; }
    public List<TenantUnitTransactionView>? tenantTransactions {  get; set; }
    public List<TenantAccount>? tenantAccounts { get; set; }

    private readonly ILogger<IndexModel> _logger;
    private readonly UserManager<User> _userManager;
    private readonly IReportingServices _reportingServices;
    public IndexModel(ILogger<IndexModel> logger, IReportingServices reportingServices, UserManager<User> userManager)
    {
        _logger = logger;
        _reportingServices = reportingServices;
        _userManager = userManager;
    }

    public async Task OnGetAsync()
    {
        try
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found. Redirecting to the login page.");
                Response.Redirect("/Identity/Account/Login");
                return;
            }

            if (User.IsInRole("Admin"))
            {
                userStats = await _reportingServices.GetUserStatsAsync();
                transactions = await _reportingServices.GetLatestTransactionsAsync();
            }
            if (User.IsInRole("Manager"))
            {
                perPropertyOccupancies = await _reportingServices.GetPerPropertyOccupancyAsync(userId);
                debtors = await _reportingServices.GetDebtorsAsync(userId);
                incomeExpenses=await _reportingServices.GetAccountsSummaryAsync(userId);
            }
            if (User.IsInRole("Tenant"))
            {
                tenantAccounts = await _reportingServices.GetTenantAccountsAsync(userId);
                tenantDebts = await _reportingServices.GetDebtsAsync(userId);
                tenantTransactions = await _reportingServices.GetLatestTenantTransactionsAsync(userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error OnGetAsync: {ex}");
            // Optionally, you can redirect to an error page or display an error message
        }
    }
}
