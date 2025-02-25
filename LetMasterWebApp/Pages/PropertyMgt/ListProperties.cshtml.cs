using LetMasterWebApp.Core;
using LetMasterWebApp.Models;
using LetMasterWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LetMasterWebApp.Pages.PropertyMgt;
[Authorize(Roles = "Admin,Manager")]
public class ListPropertiesModel : PageModel
{
    public PaginatedList<PropertyViewModel>? propertyList { get; set; }
    public List<SelectListItem>? propertyType { get; set; }

    public List<SelectListItem>? propertyManagers { get; set; }
    [BindProperty(SupportsGet = true)]
    public PropertySearchModel searchModel { get; set; } = new PropertySearchModel();
    [BindProperty]
    public PropertyCreateModel createModel { get; set; } = new PropertyCreateModel() { Name = "", PropertyManagerId = "" };
    [BindProperty]
    public PropertyUpdateModel updateModel { get; set; } = new PropertyUpdateModel();

    private readonly ILogger<ListPropertiesModel> _logger;
    private readonly IPropertyServices _propertyService;
    private readonly UserManager<User> _userManager;
    public ListPropertiesModel(ILogger<ListPropertiesModel> logger, IPropertyServices propertyService, UserManager<User> userManager)
    {
        _logger = logger;
        _propertyService = propertyService;
        _userManager = userManager;
    }

    public async Task OnGetAsync(int pageIndex = 1)
    {
        try
        {
            searchModel.PropertyManagerId = _userManager.GetUserId(User);
            propertyManagers = await _propertyService.GetPropertyManagers(searchModel.PropertyManagerId!);
            propertyType = await _propertyService.GetPropertyTypes();
            searchModel.Page = pageIndex;
            propertyList = await _propertyService.ListPropertiesAsync(searchModel);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error OnGetAsync: {ex}");
        }
    }
    public async Task<IActionResult> OnGetPropertyDetailsAsync(int id)
    {
        try
        {
            var property = await _propertyService.GetPropertyDetailsAsync(id);
            return new JsonResult(property);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error OnGetPropertyDetailsAsync: {ex}");
            return StatusCode(500, "Internal server error");
        }
    }
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            searchModel.PropertyManagerId = _userManager.GetUserId(User);
            propertyManagers = await _propertyService.GetPropertyManagers(searchModel.PropertyManagerId!);
            propertyType = await _propertyService.GetPropertyTypes();
            propertyList = await _propertyService.ListPropertiesAsync(searchModel);

        }
        catch (BadHttpRequestException ex)
        {
            throw new BadHttpRequestException(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error OnPostAsync :{ex.Message}", ex);
            return StatusCode(500, "Internal server error");
        }
        return Page();
    }
    public async Task<IActionResult> OnPostCreatePropertyAsync()
    {
        try
        {
            //if (!ModelState.IsValid)
            //    return RedirectToPage("/PropertyMgt/ListProperties", "Get");
            createModel.CreatedBy = _userManager.GetUserId(User);
            var success = await _propertyService.CreatePropertyAsync(createModel);
            if (success)
            {
                TempData["AlertMessage"] = "Property Addition Successful";
                return RedirectToPage("/PropertyMgt/ListProperties", "Get");
            }
            else
            {
                TempData["AlertMessage"] = "Property Addition Failed";
                ModelState.AddModelError(string.Empty, "Property creation failed.");
            }
        }
        catch (BadHttpRequestException ex)
        {
            _logger.LogError($"Error in OnPostCreatePropertyAsync {ex.Message}");
            TempData["AlertMessage"] = "Property Addition Failed";
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in OnPostCreatePropertyAsync {ex.Message}");
            TempData["AlertMessage"] = "Property Addition Failed";
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostUpdatePropertyAsync()
    {
        try
        {
            var success = await _propertyService.UpdatePropertyAsync(updateModel!);
            updateModel!.UpdatedBy = _userManager.GetUserId(User);
            if (success)
            {
                TempData["AlertMessage"] = "Property Update Successful";
                return RedirectToPage("/PropertyMgt/ListProperties", "Get");
            }

            else
            {
                TempData["ErrorMessage"] = "Property Update Failed";
                ModelState.AddModelError(string.Empty, "Property Update Failed");
            }

        }
        catch (BadHttpRequestException ex)
        {
            _logger.LogError($"Error in OnPostUpdatePropertyAsync {ex.Message}");
            TempData["ErrorMessage"] = "Property Update Failed";
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in OnPostUpdatePropertyAsync {ex.Message}");
            TempData["ErrorMessage"] = "Property Update Failed";
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        return RedirectToPage();
    }
}
