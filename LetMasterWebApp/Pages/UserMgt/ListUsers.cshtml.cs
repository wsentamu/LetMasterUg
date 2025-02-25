using LetMasterWebApp.Core;
using LetMasterWebApp.Models;
using LetMasterWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LetMasterWebApp.Pages.UserMgt;
[Authorize(Roles = "Admin")]
public class ListUsersModel : PageModel
{
    private readonly IUserServices _userServices;
    private readonly ILogger<ListUsersModel> _logger;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<User> _userManager;
    [BindProperty(SupportsGet = true)]
    public UserSearchModel searchModel { get; set; } = new UserSearchModel();
    [BindProperty]
    public UserCreateModel createModel { get; set; } = new UserCreateModel();
    //[BindProperty]
    //public UserUpdateModel updateModel { get; set; } = new UserUpdateModel();
    public PaginatedList<UserViewModel>? users { get; set; }
    public List<SelectListItem>? roles { get; set; }
    public ListUsersModel(IUserServices userServices, ILogger<ListUsersModel> logger, RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
    {
        _userServices = userServices;
        _logger = logger;
        _roleManager = roleManager;
        _userManager = userManager;
    }
    public async Task OnGetAsync(int pageIndex = 1)
    {
        try
        {
            PopulateRoles();
            searchModel.Page = pageIndex;
            users = await _userServices.ListUsers(searchModel);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error OnGetAsync: {ex}");
        }
    }
    public async Task<IActionResult> OnPostSearchAsync()
    {
        try
        {
            PopulateRoles();
            users = await _userServices.ListUsers(searchModel);
        }
        catch (BadHttpRequestException ex)
        {
            throw new BadHttpRequestException(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error OnPostAsync :{ex.Message}", ex);
        }
        return Page();
    }
    public async Task<IActionResult> OnPostCreateUserAsync()
    {
        try
        {
            PopulateRoles();
            createModel.MobileNumber = createModel.MobileNumber?.CleanPhone();
            createModel.PhoneNumber = createModel.PhoneNumber?.CleanPhone();
            var errCnt=ModelState.ErrorCount;
            if (errCnt>1)
            {
                _logger.LogError("Model State Issue");
                return Page();
            }
            createModel.CreatedBy = _userManager.GetUserId(User);
            var response = await _userServices.CreateUser(createModel);
            if (response.Success)
                return RedirectToPage("/UserMgt/ListUsers");
            else
                ModelState.AddModelError(string.Empty, response.ErrorMsg!);
        }
        catch (BadHttpRequestException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in OnPostCreateUserAsync {ex.Message}");
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        return Page();
    }
    public async Task<IActionResult> OnPostUpdateAsync([FromBody] UserUpdateModel model)
    {
        try
        {
            model.IsIndividual = true;
            if (!string.IsNullOrEmpty(model.OtherNamesUpdate))
                model.IsIndividual = false;
            model.UpdatedBy = _userManager.GetUserId(User);
            var success = await _userServices.EditUser(model);
            if (success)
                return new JsonResult(new { success = true });
            else
                return new JsonResult(new { success = false, message = "Edit failed" });
        }
        catch (BadHttpRequestException ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }
    public async Task<IActionResult> OnGetUserAsync(string id)
    {
        try
        {
            PopulateRoles();
            var userViewModel = await _userServices.GetUser(id);
            if (userViewModel != null)
            {
                var updateModel = new UserUpdateModel
                {
                    Id = userViewModel.Id,
                    Address = userViewModel.Address,
                    IsActive = userViewModel.IsActive,
                    Email = userViewModel.Email,
                    FirstNameUpdate = userViewModel.FirstName,
                    LastNameUpdate = userViewModel.LastName,
                    OtherNamesUpdate = userViewModel?.OtherNames,
                    MobileNumber = userViewModel!.MobileNumber,
                    PhoneNumber = userViewModel.PhoneNumber,
                    Roles = userViewModel.Roles
                };
                updateModel.AvailableRoles = await _userServices.GetAllRoles();
                if (!string.IsNullOrEmpty(userViewModel.OtherNames))
                    updateModel.IsIndividual = false;
                var result = new JsonResult(updateModel);
                return result;
            }
            return new JsonResult(new { success = false, message = "User not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in OnGetUserAsync {ex.Message}");
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }
    public async Task<IActionResult> OnGetUserDetailsAsync(string id)
    {
        try
        {
            var userViewModel = await _userServices.GetUser(id);
            if (userViewModel != null)
            {
                var result = new JsonResult(userViewModel);
                return result;
            }
            return new JsonResult(new { success = false, message = "Failed to fetch user details" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in OnGetUserDetailsAsync {ex.Message}");
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }
    private void PopulateRoles()
    {
        roles = _roleManager.Roles.Select(r => new SelectListItem
        {
            Value = r.Name,
            Text = r.Name
        }).ToList();
    }
}
