using LetMasterWebApp.Core;
using LetMasterWebApp.Models;
using LetMasterWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LetMasterWebApp.Pages.Reports;
[Authorize(Roles = "Admin")]
public class NotificationsModel : PageModel
{
    public PaginatedList<UserMessage>? Messages { get; set; }
    [BindProperty(SupportsGet = true)]
    public ReportSearchModel? searchModel { get; set; } = new ReportSearchModel();
    private readonly UserManager<User> _userManager;
    private readonly IReportingServices _reportingService;
    public NotificationsModel(UserManager<User> userManager, IReportingServices reportingService)
    {
        _userManager = userManager;
        _reportingService = reportingService;
    }

    public void OnGet()
    {
    }
    public async Task OnPostAsync()
    {
        Messages = await _reportingService.GetNotificationsAsync(searchModel);
    }
}
