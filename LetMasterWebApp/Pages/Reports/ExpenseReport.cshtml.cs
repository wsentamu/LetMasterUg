using LetMasterWebApp.Core;
using LetMasterWebApp.Models;
using LetMasterWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LetMasterWebApp.Pages.Reports;
[Authorize(Roles = "Manager,Admin")]
public class ExpenseReportModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public ReportSearchModel? searchModel { get; set; } = new ReportSearchModel();
    public List<SelectListItem>? properties { get; set; }
    public PropertyExpense? expenses { get; set; }// = new PropertyExpense();
    //service injection
    private readonly UserManager<User> _userManager;
    private readonly IReportingServices _reportingService;
    public ExpenseReportModel(UserManager<User> userManager, IReportingServices reportingService)
    {
        _userManager = userManager;
        _reportingService = reportingService;
    }

    public async Task OnGetAsync()
    {
        searchModel!.UserId = _userManager.GetUserId(User);
        properties = await _reportingService.GetPropertySelectList(searchModel.UserId!);
    }
    public async Task OnPostAsync()
    {
        searchModel!.UserId = _userManager.GetUserId(User);
        properties = await _reportingService.GetPropertySelectList(searchModel.UserId!);
        expenses = await _reportingService.GetPropertyExpensesAsync(searchModel);
    }
    public async Task<IActionResult> OnPostExportAsync()
    {
        searchModel!.UserId = _userManager.GetUserId(User);
        properties = await _reportingService.GetPropertySelectList(searchModel.UserId!);
        expenses = await _reportingService.GetPropertyExpensesAsync(searchModel);
        return DataExportHelper.ExportToExcel(expenses.Expenses, $"ExpenseReport-{expenses.PropertyName}.xlsx");
    }
}
