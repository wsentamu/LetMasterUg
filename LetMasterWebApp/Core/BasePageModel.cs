using LetMasterWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LetMasterWebApp.Core;
public class BasePageModel:PageModel
{
    protected readonly UserManager<User> _userManager;
    protected readonly SignInManager<User> _signInManager;

    public BasePageModel(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }
}
