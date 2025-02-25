using Hangfire;
using LetMasterWebApp.Core;
using LetMasterWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace LetMasterWebApp.Pages;
public class SupportModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly INotificationService _notificationService;
    private readonly IBackgroundJobClient _background;
    public SupportModel(IConfiguration configuration, INotificationService notificationService, IBackgroundJobClient background)
    {
        _configuration = configuration;
        _notificationService = notificationService;
        _background = background;
    }
    [BindProperty]
    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 5)]
    public string Subject { get; set; } = default!;
    [BindProperty]
    [Required]
    [StringLength(1000, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 10)]
    public string Message { get; set; } = default!;
    public void OnGet()
    {
    }
    public IActionResult OnPost()
    {
        try
        {
            if (ModelState.IsValid)
            {
                var supportMail = _configuration.GetValue<string>("NotificationSettings:SupportEmail");
                _background.Enqueue(()=> _notificationService.SendEmailAsync(supportMail!, Subject, Message));
                TempData["AlertMessage"] = "Support request submitted";
                return RedirectToAction("Support");
            }
        }
        catch
        {
            TempData["ErrorMessage"] = "Support request failed to submit";
        }
        return Page();
    }
}
