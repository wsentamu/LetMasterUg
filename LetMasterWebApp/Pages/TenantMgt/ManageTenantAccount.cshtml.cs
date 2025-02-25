using LetMasterWebApp.Core;
using LetMasterWebApp.Models;
using LetMasterWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LetMasterWebApp.Pages.TenantMgt;
[Authorize]
public class ManageTenantAccountModel : PageModel
{
    public PaginatedList<TenantUnitTransaction>? transactionList { get; set; }
    [BindProperty(SupportsGet = true)]
    public TenantUnitTransactionSearchModel? transactionSearch { get; set; } = new TenantUnitTransactionSearchModel();
    [BindProperty]
    public TenantUnitTransactionCreateModel? transactionCreate { get; set; } = new TenantUnitTransactionCreateModel { Amount = 0, Description = string.Empty, TenantUnitId = 0, TransactionDate = DateTime.Now, TransactionType = "D" };
    [BindProperty]
    public MobileMoneyCreateModel? mobileMoneyCreate { get; set; } = new MobileMoneyCreateModel { Amount = 0, DebitNumber = "", TenantUnitId = 0 };
    [BindProperty]
    public TenantUnitUpdateModel? accountUpdate { get; set; } = new TenantUnitUpdateModel { Id = 0 };
    public TenantUnitViewDetail? accountDetails { get; set; }

    private readonly ILogger<ManageTenantAccountModel> _logger;
    private readonly ITenantServices _tenantServices;
    private readonly UserManager<User> _userManager;
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;
    public ManageTenantAccountModel(ILogger<ManageTenantAccountModel> logger, ITenantServices tenantServices, UserManager<User> userManager, IPaymentService paymentService, IConfiguration configuration)
    {
        _logger = logger;
        _tenantServices = tenantServices;
        _userManager = userManager;
        _paymentService = paymentService;
        _configuration = configuration;
    }
    public async Task<IActionResult> OnGetAsync(int id, int pageIndex = 1)
    {
        accountDetails = await _tenantServices.GetTenantAccountDetailsAsync(id);
        transactionSearch!.TenantUnitId = id;
        transactionSearch.Page = pageIndex;
        transactionCreate!.CreatedBy = _userManager.GetUserId(User);
        transactionCreate!.TenantUnitId = id;
        accountUpdate!.Id = id;
        accountUpdate.AgreedRate = accountDetails.AgreedRate;
        transactionList = await _tenantServices.GetTransactionsAsync(transactionSearch);
        mobileMoneyCreate!.TenantUnitId = id;
        return Page();
    }
    public async Task<IActionResult> OnPostUpdateTenantAccountAsync()
    {
        try
        {
            var state = ModelState.IsValid;
            accountUpdate!.UpdatedBy = _userManager.GetUserId(User);
            var success = await _tenantServices.UpdateTenantAccountAsync(accountUpdate);
            if (success)
            {
                TempData["AlertMessage"] = "Account Update Successful";
                return RedirectToPage();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Account Update Failed.");
                TempData["ErrorMessage"] = "Account Update Failed";
            }
        }
        catch (BadHttpRequestException ex)
        {
            TempData["ErrorMessage"] = "Account Update Failed";
            _logger.LogError($"Error OnPostUpdateTenantAccountAsync: {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Account Update Failed";
            _logger.LogError($"Error OnPostUpdateTenantAccountAsync: {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
        return Page();
    }
    public async Task<IActionResult> OnPostPostTransactionAsync()
    {
        try
        {
            var state = ModelState.IsValid;
            transactionCreate!.CreatedBy = _userManager.GetUserId(User);
            var success = await _tenantServices.PostTransactionAsync(transactionCreate);
            if (success)
            {
                TempData["AlertMessage"] = "Post Transaction Successful";
                return RedirectToPage();
            }
            else
            {
                TempData["ErrorMessage"] = "Post Transaction Failed";
                ModelState.AddModelError(string.Empty, "Post Transaction Failed.");
            }
        }
        catch (BadHttpRequestException ex)
        {
            TempData["ErrorMessage"] = "Post Transaction Failed";
            _logger.LogError($"Error OnPostPostTransactionAsync: {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Post Transaction Failed";
            _logger.LogError($"Error OnPostPostTransactionAsync: {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
        return Page();
    }
    public async Task<IActionResult> OnPostMmDebitRequestAsync()
    {
        try
        {
            var state = ModelState.IsValid;
            mobileMoneyCreate!.CreatedBy = _userManager.GetUserId(User);
            var apiResp = await _paymentService.UssdPushAsync(mobileMoneyCreate);
            if (apiResp!=null&& apiResp.status!=null &&apiResp.status.response_code!=null && (apiResp.status!.response_code!.ToUpper()== _configuration["IntergrationSettings:ProcessingTransaction"] ||apiResp.status.response_code.ToUpper()== _configuration["IntergrationSettings:SuccessDebitRequest"]))
            {
                TempData["AlertMessage"] = "Mobile Money Initiation Successful";
                return RedirectToPage();
            }
            else
            {
                TempData["ErrorMessage"] = "Mobile Money Initiation Failed";
                ModelState.AddModelError(string.Empty, "Mobile Money Initiation Failed.");
            }
        }
        catch (BadHttpRequestException ex)
        {
            TempData["ErrorMessage"] = "Mobile Money Initiation Failed";
            _logger.LogError($"Error OnPostMmDebitRequestAsync: {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Mobile Money Initiation Failed";
            _logger.LogError($"Error OnPostMmDebitRequestAsync: {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
        return RedirectToPage();
    }
}
