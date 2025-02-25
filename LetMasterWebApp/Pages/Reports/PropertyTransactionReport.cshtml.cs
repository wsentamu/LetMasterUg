using LetMasterWebApp.Core;
using LetMasterWebApp.Models;
using LetMasterWebApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LetMasterWebApp.Pages.Reports;
public class PropertyTransactionReportModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public ReportSearchModel? searchModel { get; set; } = new ReportSearchModel();
    public List<SelectListItem>? properties { get; set; }
    public PropertyRentTransaction? rentTransaction { get; set; }
    private readonly UserManager<User> _userManager;
    private readonly IReportingServices _reportingService;
    public PropertyTransactionReportModel(UserManager<User> userManager, IReportingServices reportingService)
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
        rentTransaction = await _reportingService.GetRentTransactionAsync(searchModel);
    }
    public async Task<IActionResult> OnPostExportAsync()
    {
        searchModel!.UserId = _userManager.GetUserId(User);
        properties = await _reportingService.GetPropertySelectList(searchModel.UserId!);
        rentTransaction = await _reportingService.GetRentTransactionAsync(searchModel);
        return DataExportHelper.ExportToExcel(rentTransaction.TransactionEntries!, $"RentTransactionReport-{rentTransaction.PropertyName}.xlsx");
    }
}
