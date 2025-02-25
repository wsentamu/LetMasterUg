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
public class ListTenantsModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public TenantUnitSearchModel searchModel { get; set; } = new TenantUnitSearchModel();
    public PaginatedList<TenantUnitViewModel>? tenantList { get; set; }
    public List<SelectListItem>? properties { get; set; }
    public List<SelectListItem>? tenants { get; set; }
    [BindProperty]
    public TenantUnitCreateModel createModel { get; set; }=new TenantUnitCreateModel();
    private readonly ILogger<ListTenantsModel> _logger;
    private readonly ITenantServices _tenantServices;
    private readonly UserManager<User> _userManager;
    public ListTenantsModel(ILogger<ListTenantsModel> logger, ITenantServices tenantServices, UserManager<User> userManager)
    {
        _logger = logger;
        _tenantServices = tenantServices;
        _userManager = userManager;
    }

    public async Task OnGetAsync(int pageIndex = 1)
    {
        try
        {
            searchModel.PropertyManagerId = _userManager.GetUserId(User);
            searchModel.Page = pageIndex;
            tenantList = await _tenantServices.ListTenantAccountsAsync(searchModel);
            properties=await _tenantServices.GetPropertySelectList(searchModel.PropertyManagerId!);
            tenants=await _tenantServices.GetTenantsAsync(searchModel.PropertyManagerId!);
            createModel.CreatedBy=searchModel.PropertyManagerId;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error OnGetAsync: {ex}");
        }
    }
    public async Task<IActionResult> OnGetVacantUnitsAsync(int propertyId)
    {
        try
        {
            var vacantUnits = await _tenantServices.GetVacantUnitsAsync(propertyId);
            return new JsonResult(vacantUnits);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error OnGetVacantUnitsAsync: {ex}");
            return StatusCode(500, "Internal server error");
        }
    }
    public async Task<IActionResult> OnPostCreateUnitAccAsync()
    {
        try
        {
            var state = ModelState.IsValid;
            createModel.CreatedBy = _userManager.GetUserId(User);
            var success = await _tenantServices.CreateTenantUnitAccountAsync(createModel);
            if (success)
            {
                TempData["AlertMessage"] = "Tenant Account Creation Successful";
                return RedirectToPage("/TenantMgt/ListTenants");
            }
                
            else
            {
                TempData["ErrorMessage"] = "Tenant Account Creation Failed";
                ModelState.AddModelError(string.Empty, "Property creation failed.");
            }
                
        }
        catch (BadHttpRequestException ex)
        {
            TempData["ErrorMessage"] = "Tenant Account Creation Failed";
            _logger.LogError($"Error OnPostCreateUnitAccAsync: {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Tenant Account Creation Failed";
            _logger.LogError($"Error OnPostCreateUnitAccAsync: {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
        return Page();
    }
}
