using LetMasterWebApp.Core;
using LetMasterWebApp.Models;
using LetMasterWebApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LetMasterWebApp.Pages.Reports;

public class TenantAccountStatementModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public ReportSearchModel searchModel { get; set; } = new ReportSearchModel();
    public AccountStatement accountStatement { get; set; } = new AccountStatement();
    public List<SelectListItem>? accounts { get; set; }
    //service injection
    private readonly UserManager<User> _userManager;
    private readonly IReportingServices _reportingService;
    public TenantAccountStatementModel(UserManager<User> userManager, IReportingServices reportingService)
    {
        _userManager = userManager;
        _reportingService = reportingService;
    }

    public async Task OnGetAsync(int id)
    {
        searchModel.UserId = _userManager.GetUserId(User);
        searchModel.TenantAccountId = id;
        accounts = await _reportingService.GetAccountsSelectList(searchModel.UserId!);
    }
    public async Task OnPostAsync()
    {
        searchModel.UserId = _userManager.GetUserId(User);
        accounts = await _reportingService.GetAccountsSelectList(searchModel.UserId!);
        accountStatement = await _reportingService.GetAccountTransactionsAsync(searchModel);
    }
    public async Task<IActionResult> OnPostExportAsync()
    {
        searchModel!.UserId = _userManager.GetUserId(User);
        accounts = await _reportingService.GetAccountsSelectList(searchModel.UserId!);
        accountStatement = await _reportingService.GetAccountTransactionsAsync(searchModel);
        return DataExportHelper.ExportToExcel(accountStatement.AccountTransactions!, $"AccountStatement-{accountStatement.PropertyName}-{accountStatement.UnitNo}.xlsx");
    }
}
