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
public class ManagePropertyModel : PageModel
{
    public PaginatedList<UnitViewModel>? unitList { get; set; }
    public List<SelectListItem>? unitTypes { get; set; }
    public List<SelectListItem>? units { get; set; }
    public List<SelectListItem>? expenseTypes { get; set; }
    [BindProperty(SupportsGet = true)]
    public UnitSearchModel? unitSearch { get; set; } = new UnitSearchModel();
    [BindProperty]
    public UnitCreateModel unitCreate { get; set; } = new UnitCreateModel();
    [BindProperty]
    public UnitUpdateModel unitUpdate { get; set; } = new UnitUpdateModel();
    [BindProperty]
    public ExpenseCreateModel expenseCreate { get; set; } = new ExpenseCreateModel();

    private readonly UserManager<User> _userManager;
    private readonly ILogger<ManagePropertyModel> _logger;
    private readonly IPropertyServices _propertyServices;
    public ManagePropertyModel(ILogger<ManagePropertyModel> logger, IPropertyServices propertyServices, UserManager<User> userManager)
    {
        _logger = logger;
        _propertyServices = propertyServices;
        _userManager = userManager;
    }
    public PropertyViewModel? property { get; set; }
    public UnitViewModel? unit { get; set; }
    public async Task<IActionResult> OnGetAsync(int id, int pageIndex = 1)
    {
        property = await _propertyServices.GetPropertyDetailsAsync(id);
        unitSearch!.PropertyId = id;
        unitCreate!.PropertyId = id;
        unitTypes = await _propertyServices.GetUnitTypes();
        unitSearch.Page = pageIndex;
        unitList = await _propertyServices.ListUnitsAsync(unitSearch);
        expenseTypes = await _propertyServices.GetExpensetypes();
        units = await _propertyServices.GetUnitsByProperty(id);
        expenseCreate.PropertyId = id;
        return Page();
    }
    public async Task<IActionResult> OnPostCreateUnitAsync()
    {
        try
        {
            //if (!ModelState.IsValid)
            //    return RedirectToPage("/PropertyMgt/ListProperties", "Get");
            unitSearch!.PropertyId = unitCreate.PropertyId;
            unitCreate!.PropertyId = unitCreate.PropertyId;
            unitTypes = await _propertyServices.GetUnitTypes();
            unitList = await _propertyServices.ListUnitsAsync(unitSearch);
            unitCreate.CreatedBy = _userManager.GetUserId(User);
            var success = await _propertyServices.CreateUnitAsync(unitCreate);
            property = await _propertyServices.GetPropertyDetailsAsync(unitCreate.PropertyId);
            units = await _propertyServices.GetUnitsByProperty(unitCreate.PropertyId);
            if (success)
            {
                TempData["AlertMessage"] = "Unit Addition Successful";
                return RedirectToPage();
            }
            else
            {
                TempData["AlertMessage"] = "Unit Addition Failed";
                ModelState.AddModelError(string.Empty, "Unit creation failed.");
            }

        }
        catch (BadHttpRequestException ex)
        {
            _logger.LogError($"Error in OnPostCreateUnitAsync {ex.Message}");
            TempData["AlertMessage"] = "Unit Addition Failed";
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in OnPostCreateUnitAsync {ex.Message}");
            TempData["AlertMessage"] = "Unit Addition Failed";
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        return RedirectToPage();
    }
    public async Task<IActionResult> OnGetUnitDetailsAsync(int id)
    {
        try
        {
            unit = await _propertyServices.GetUnitDetailsAsync(id);
            return new JsonResult(unit);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error OnGetUnitDetailsAsync: {ex}");
            return StatusCode(500, "Internal server error");
        }
    }
    public async Task<IActionResult> OnPostUpdateUnitAsync()
    {
        try
        {
            unitUpdate!.UpdatedBy = _userManager.GetUserId(User);
            var success = await _propertyServices.UpdateUnitAsync(unitUpdate!);
            if (success)
            {
                TempData["AlertMessage"] = "Unit Update Successful";
                return new JsonResult(new { success });
            }

            else
            {
                TempData["ErrorMessage"] = "Unit Addition Failed"; 
                return new JsonResult(new { success = false, message = "Unit Update Failed" });
            }

        }
        catch (BadHttpRequestException ex)
        {
            _logger.LogError($"Error in OnPostUpdateUnitAsync {ex.Message}");
            TempData["ErrorMessage"] = "Unit Addition Failed";
            return new JsonResult(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in OnPostUpdateUnitAsync {ex.Message}");
            TempData["ErrorMessage"] = "Unit Addition Failed";
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }
    public async Task<IActionResult> OnPostAddExpenseAsync()
    {
        try
        {
            expenseCreate.CreatedBy = _userManager.GetUserId(User);
            var success = await _propertyServices.AddExpenseAsync(expenseCreate);
            if (success)
                return new JsonResult(new { success });
            else
                return new JsonResult(new { success = false, message = "Add Expense Failed" });
        }
        catch (BadHttpRequestException ex)
        {
            _logger.LogError($"Error in OnPostAddExpenseAsync {ex.Message}");
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }
}
