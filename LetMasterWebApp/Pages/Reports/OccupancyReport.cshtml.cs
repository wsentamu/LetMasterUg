using LetMasterWebApp.Core;
using LetMasterWebApp.Models;
using LetMasterWebApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LetMasterWebApp.Pages.Reports;
public class OccupancyReportModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public ReportSearchModel? searchModel { get; set; } = new ReportSearchModel();
    public List<SelectListItem>? properties { get; set; }
    public PropertyOccupancyReport? occupancyReport { get; set; }// = new PropertyOccupancyReport();
    //service injection
    private readonly UserManager<User> _userManager;
    private readonly IReportingServices _reportingService;
    public OccupancyReportModel(UserManager<User> userManager, IReportingServices reportingService)
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
        occupancyReport = await _reportingService.GetOccupancyReportAsync(searchModel);
    }
    public async Task<IActionResult> OnPostExportAsync()
    {
        searchModel!.UserId = _userManager.GetUserId(User);
        properties = await _reportingService.GetPropertySelectList(searchModel.UserId!);
        occupancyReport = await _reportingService.GetOccupancyReportAsync(searchModel);
        return DataExportHelper.ExportToExcel(occupancyReport.UnitDetails!, $"OccupancyReport-{occupancyReport.PropertyName}.xlsx");
    }
}
