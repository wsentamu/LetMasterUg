using LetMasterWebApp.Core;
using LetMasterWebApp.Models;
using LetMasterWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LetMasterWebApp.Pages.TenantMgt;
[Authorize]
public class ListTenantAccountsModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public TenantUnitSearchModel searchModel { get; set; } = new TenantUnitSearchModel();
    public PaginatedList<TenantUnitViewModel>? tenantAccountList { get; set; }
    //Service injection
    private readonly ILogger<ListTenantAccountsModel> _logger;
    private readonly ITenantServices _tenantServices;
    private readonly UserManager<User> _userManager;
    public ListTenantAccountsModel(ILogger<ListTenantAccountsModel> logger, ITenantServices tenantServices, UserManager<User> userManager)
    {
        _logger = logger;
        _tenantServices = tenantServices;
        _userManager = userManager;
    }

    public async Task OnGetAsync(int pageIndex = 1)
    {
        try
        {
            searchModel.TenantUserId = _userManager.GetUserId(User);
            searchModel.Page = pageIndex;
            tenantAccountList = await _tenantServices.ListTenantAccountsAsync(searchModel);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error OnGetAsync: {ex}");
        }
    }
}
