using LetMasterWebApp.Core;
using LetMasterWebApp.Models;
using LetMasterWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LetMasterWebApp.Pages.Reports;
[Authorize(Roles = "Manager")]
public class AccountStatementModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public ReportSearchModel searchModel { get; set; } = new ReportSearchModel();
    public AccountStatement accountStatement { get; set; }= new AccountStatement();
    public List<SelectListItem>? properties { get; set; }
    public List<SelectListItem>? accounts { get; set; }
    //service injection
    private readonly UserManager<User> _userManager;
    private readonly IReportingServices _reportingService;
    public AccountStatementModel(UserManager<User> userManager, IReportingServices reportingService)
    {
        _userManager = userManager;
        _reportingService = reportingService;
    }

    public async Task OnGetAsync()
    {
        searchModel.UserId = _userManager.GetUserId(User);
        properties = await _reportingService.GetPropertySelectList(searchModel.UserId!);
    }
    public async Task OnPostAsync()
    {
        searchModel.UserId = _userManager.GetUserId(User);
        properties = await _reportingService.GetPropertySelectList(searchModel.UserId!);
        accountStatement = await _reportingService.GetTenantAccountStatementAsync(searchModel);
    }
    public async Task<IActionResult> OnGetTenantAccountsAsync(int propertyId)
    {
        try
        {
            var tenantAccounts = await _reportingService.GetTenantAccountsSelectList(propertyId);
            return new JsonResult(tenantAccounts);
        }
        catch
        {
            return StatusCode(500, "Internal server error");
        }
    }
    public async Task<IActionResult> OnPostExportAsync()
    {
        searchModel!.UserId = _userManager.GetUserId(User);
        properties = await _reportingService.GetPropertySelectList(searchModel.UserId!);
        accountStatement = await _reportingService.GetTenantAccountStatementAsync(searchModel);
        return DataExportHelper.ExportToExcel(accountStatement.AccountTransactions!, $"AccountStatement-{accountStatement.Tenant}-{accountStatement.UnitNo}.xlsx");
    }
}
