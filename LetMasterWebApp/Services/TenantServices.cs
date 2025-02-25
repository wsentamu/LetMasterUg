using AutoMapper;
using Hangfire;
using LetMasterWebApp.Core;
using LetMasterWebApp.DataLayer;
using LetMasterWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;

namespace LetMasterWebApp.Services;
public interface ITenantServices
{
    public Task<PaginatedList<TenantUnitViewModel>> ListTenantAccountsAsync(TenantUnitSearchModel searchModel);
    public Task<List<SelectListItem>> GetPropertySelectList(string userId);
    public Task<IActionResult> GetVacantUnitsAsync(int propertyId);
    public Task<List<SelectListItem>> GetTenantsAsync(string propertyManagerId);
    public Task<bool> CreateTenantUnitAccountAsync(TenantUnitCreateModel model);
    public Task<TenantUnitViewDetail> GetTenantAccountDetailsAsync(int tenantUnitId);
    public Task<PaginatedList<TenantUnitTransaction>> GetTransactionsAsync(TenantUnitTransactionSearchModel searchModel);
    public Task<bool> PostTransactionAsync(TenantUnitTransactionCreateModel transactionCreateModel);
    public Task<bool> UpdateTenantAccountAsync(TenantUnitUpdateModel model);
}
public class TenantServices : ITenantServices
{
    private readonly ILogger<TenantServices> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    private readonly IConfiguration _config;
    private readonly LinkGenerator _linkGenerator;
    private readonly INotificationService _notificationService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    public TenantServices(ILogger<TenantServices> logger, ApplicationDbContext context, UserManager<User> userManager, IMapper mapper, IConfiguration config, LinkGenerator linkGenerator, INotificationService notificationService, IBackgroundJobClient backgroundJobClient)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
        _config = config;
        _linkGenerator = linkGenerator;
        _notificationService = notificationService;
        _backgroundJobClient = backgroundJobClient;
    }
    public async Task<List<SelectListItem>> GetPropertySelectList(string userId)
    {
        try
        {
            return await _context.Properties.Where(p => p.IsActive && p.PropertyManagerId == userId)
                .Select(property => new SelectListItem
                {
                    Text = property.Name,
                    Value = property.Id.ToString()
                }).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetPropertySelectList: {userId} exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<PaginatedList<TenantUnitViewModel>> ListTenantAccountsAsync(TenantUnitSearchModel searchModel)
    {
        _logger.LogInformation($"ListTenantAccountsAsync: {JsonSerializer.Serialize(searchModel)}");
        try
        {
            if (searchModel == null)
                throw new BadHttpRequestException("No data submitted");
            var query = from tu in _context.TenantUnits
                        join t in _context.Tenants on tu.TenantId equals t.Id
                        join u in _context.Units on tu.UnitId equals u.Id
                        join p in _context.Properties on u.PropertyId equals p.Id
                        where tu.IsActive == true
                        select new TenantUnitViewModel
                        {
                            TenantUnitId = tu.Id,
                            TenantUserId = t.UserId,
                            PropertyId = p.Id,
                            PropertyManagerId = p.PropertyManagerId,
                            TenantId = t.Id,
                            TenantName = t.Name,
                            TenantMobile = t.MobileNumber,
                            PropertyName = p.Name,
                            UnitId = u.Id,
                            UnitName = u.Name,
                            StartDate = tu.StartDate,
                            AgreedRate = tu.AgreedRate ?? 0,
                            CurrentBalance = tu.CurrentBalance ?? 0
                        };
            if (!string.IsNullOrEmpty(searchModel.PropertyManagerId))
                query = query.Where(p => p.PropertyManagerId == searchModel.PropertyManagerId);
            if (!string.IsNullOrEmpty(searchModel.TenantName))
                query = query.Where(t => t.TenantName!.Contains(searchModel.TenantName));
            if (!string.IsNullOrEmpty(searchModel.UnitName))
                query = query.Where(u => u.UnitName!.Contains(searchModel.UnitName));
            if (searchModel.PropertyId != null)
                query = query.Where(p => p.PropertyId == searchModel.PropertyId);
            if (searchModel.TenantId != null)
                query = query.Where(t => t.TenantId == searchModel.TenantId);
            if (!string.IsNullOrEmpty(searchModel.TenantUserId))
                query = query.Where(p => p.TenantUserId == searchModel.TenantUserId);
            var paginatedData = await PaginationHelper.PaginateAsync(query, searchModel.Page, searchModel.PageSize);
            return paginatedData;
        }
        catch (Exception ex)
        {
            _logger.LogError($"ListTenantAccountsAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    //get unoccupied units by property id
    public async Task<IActionResult> GetVacantUnitsAsync(int propertyId)
    {
        var units = await _context.Units
            .Where(u => u.PropertyId == propertyId && u.IsActive && !u.IsOccupied)
            .Select(u => new { value = u.Id, text = u.Name, standardRate = u.StandardRate })
            .ToListAsync();

        return new JsonResult(units);
    }
    //get current active tenants (if existing tenant is to ocuppy additional unit)
    public async Task<List<SelectListItem>> GetTenantsAsync(string propertyManagerId)
    {
        try
        {
            var tenants = await _context.Tenants
            .Where(t => _context.TenantUnits
                .Any(tu => tu.TenantId == t.Id &&
                           _context.Units
                               .Any(u => u.Id == tu.UnitId &&
                                         _context.Properties
                                             .Any(p => p.Id == u.PropertyId && p.PropertyManagerId == propertyManagerId))))
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.Name
            })
            .Distinct()
            .ToListAsync();
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetTenants: {propertyManagerId} exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<bool> CreateTenantUnitAccountAsync(TenantUnitCreateModel model)
    {
        /*
         * * CREATE TENANT
         * * 1- if create account true, build UserCreateModel create user, get userId
         * * 2- Create tenant. Name = Generated Display name or Generate in case no account created
         * * 3- Create TenantUnit
         * * 4- Basing on bill start date create debits for each month using agreed rate and update Current Balance
         * * 5- If Deposit amount >0 create credit entry and update Current Balance
         * * 6- Update account with current balance; END
         */
        _logger.LogInformation($"CreateTenantAccount: {JsonSerializer.Serialize(model)}");
        try
        {
            if (model == null)
                throw new BadHttpRequestException("No Data Submitted");
            if (model.IsExistingTenant && model.TenantId == 0)
                throw new BadHttpRequestException("No Tenant Submited");
            if (!model.IsExistingTenant && model.IsIndividual && (string.IsNullOrEmpty(model.FirstName) || string.IsNullOrEmpty(model.LastName)))
                throw new BadHttpRequestException($"Indivudual First and Last Names Required");
            if (!model.IsExistingTenant && !model.IsIndividual && string.IsNullOrEmpty(model.OtherNames))
                throw new BadHttpRequestException("Entity Name is Required");
            if (!model.IsExistingTenant && model.CreateAccount && string.IsNullOrEmpty(model.Email))
                throw new BadHttpRequestException("Username and Email Required");
            if (model.UnitId == 0)
                throw new BadHttpRequestException("Unit number Required");
            if (model.AgreedRate <= 0)
                throw new BadHttpRequestException("Agreed Amount Required");
            var tenantName = string.Empty;
            if (model.IsIndividual)
                tenantName = $"{model.FirstName} {model.LastName}";
            else
                tenantName = $"{model.OtherNames}";
            var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == model.UnitId && u.IsActive == true);
            if (unit == null)
                throw new BadHttpRequestException("Unit not found");
            var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == unit.PropertyId);
            if (property == null)
                throw new BadHttpRequestException("Property not found");
            var tenancy = $"Property: {property.Name}<br/>Location: {property.Address}<br/>Unit No.: {unit.Name}<br/>Rate: {model.AgreedRate.ToString("C")}";
            if (model.CreateAccount)
            {
                model.UserName = model.Email;
                //create new tenant account
                var exist = await _userManager.FindByNameAsync(model.UserName!);
                if (exist != null)
                    throw new BadHttpRequestException($"User {model.UserName} Already Exists");
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!@#$%&()-_*";
                string schar = "!@#$%&()-_*";
                string nos = "0123456789";
                var random = new Random();
                string p0 = new string(Enumerable.Repeat(schar, 2)
                  .Select(s => s[random.Next(s.Length)]).ToArray());
                string p1 = new string(Enumerable.Repeat(chars, 3)
                  .Select(s => s[random.Next(s.Length)]).ToArray());
                string p2 = new string(Enumerable.Repeat(nos, 3)
                  .Select(s => s[random.Next(s.Length)]).ToArray());
                string p3 = new string(Enumerable.Repeat(chars, 3)
                  .Select(s => s[random.Next(s.Length)]).ToArray());
                string p4 = new string(Enumerable.Repeat(nos, 2)
                  .Select(s => s[random.Next(s.Length)]).ToArray());
                string password = p1 + p2 + p3 + p0 + p4;
                var user = new User
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    OtherNames = model.OtherNames,
                    MobileNumber = model.MobileNumber,
                    PhoneNumber = model.PhoneNumber,
                    DisplayName = tenantName,
                    IsActive = true,
                };
                var userAccResult = await _userManager.CreateAsync(user, password);
                if (userAccResult.Succeeded)
                {
                    model.UserId = user.Id;
                    var roleRes = await _userManager.AddToRoleAsync(user, "TENANT");
                    if (!roleRes.Succeeded)
                        throw new BadHttpRequestException("Failed To Create User Account");
                }
                else
                    throw new BadHttpRequestException("Failed To Assign Role");
                //notify tenant
                //send email to new user with username, link
                var domainString = _config.GetValue<string>("DomainUrl");
                var hostString = new HostString(domainString!);
                var indexUrl = _linkGenerator.GetUriByPage(
                    page: "/Index",
                    handler: null,
                    values: null,
                    scheme: "https",
                    host: hostString);
                var subject = $"New Ug Let Master User Account";
                var body = _config.GetValue<string>("NotificationTemplates:TenantWelcomeEmailTemplate");
                if (!string.IsNullOrEmpty(body))
                    body = body.Replace("{FULLNAME}", user.DisplayName).Replace("{LINK}", indexUrl).Replace("{PASS}", password).Replace("{TENANCY}", tenancy);
                var footer = _config.GetValue<string>("NotificationTemplates:FooterTemplate");
                if (!string.IsNullOrEmpty(footer))
                    footer = footer.Replace("{YEAR}", DateTime.Now.Year.ToString());
                body += footer;
                if (!string.IsNullOrEmpty(model.Email) && !string.IsNullOrEmpty(body))
                    _backgroundJobClient.Enqueue(() => _notificationService.SendEmailAsync(model.Email, subject, body));
                var smsBody = _config.GetValue<string>("NotificationTemplates:TenantWelcomeEmailTemplate");
                var mobile = model.MobileNumber;
                if (string.IsNullOrEmpty(mobile))
                    mobile = model.PhoneNumber;
                if (!string.IsNullOrEmpty(smsBody) && !string.IsNullOrEmpty(mobile))
                {
                    smsBody = smsBody.Replace("{FULLNAME}", user.DisplayName).Replace("{EMAIL}", model.Email);
                    _backgroundJobClient.Enqueue(() => _notificationService.SendSms(mobile, smsBody));
                }
            }
            if (!model.IsExistingTenant)
            {
                //create new tenant, set tenant Id after ef SaveChangesAsync
                var tenant = new Tenant
                {
                    IsActive = true,
                    CreatedBy = model.CreatedBy,
                    CreatedDate = DateTime.Now,
                    Email = model.Email,
                    MobileNumber = model.MobileNumber,
                    PhoneNumber = model.PhoneNumber,
                    Name = tenantName,
                    UserId = model.UserId,
                };
                await _context.Tenants.AddAsync(tenant);
                await _context.SaveChangesAsync();
                model.TenantId = tenant.Id;
            }
            //create tenant-unit account
            var tenantUnit = new TenantUnit
            {
                AgreedRate = model.AgreedRate,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CurrentBalance = 0,
                StartDate = model.StartDate,
                TenantId = model.TenantId,
                UnitId = model.UnitId,
                CreatedBy = model.CreatedBy,
            };
            await _context.TenantUnits.AddAsync(tenantUnit);
            if (unit != null)
                unit.IsOccupied = true;
            await _context.SaveChangesAsync();
            //create initial bills (base on effective date)
            decimal balance = 0;
            if (model.StartDate < DateTime.UtcNow)
            {
                DateTime currentDate = DateTime.UtcNow;
                DateTime startDate = model.StartDate;
                var billCount = 0;
                var bills = new List<TenantUnitTransaction>();
                while (startDate < currentDate)
                {
                    var bill = new TenantUnitTransaction
                    {
                        TenantUnitId = tenantUnit.Id,
                        Amount = model.AgreedRate,
                        IsActive = true,
                        TransactionType = "D",
                        Description = $"BILL {startDate.Year}-{startDate.Month}",
                        TransactionMode = "SYSTEM",
                        TransactionRef = $"D{tenantUnit.Id}-{startDate.Year}{startDate.Month}",
                        CreatedDate = DateTime.UtcNow,
                        TransactionDate = DateTime.UtcNow,
                        CreatedBy = model.CreatedBy,
                    };
                    bills.Add(bill);
                    billCount++;
                    startDate = startDate.AddMonths(1);
                }
                await _context.TenantUnitTransactions.AddRangeAsync(bills);
                await _context.SaveChangesAsync();
                balance = billCount * model.AgreedRate;
            }
            if (model.DepositAmount > 0)
            {
                //credit account
                var credit = new TenantUnitTransaction
                {
                    TenantUnitId = tenantUnit.Id,
                    Amount = model.DepositAmount,
                    IsActive = true,
                    TransactionType = "C",
                    Description = "Deposit",
                    CreatedDate = DateTime.UtcNow,
                    TransactionDate = DateTime.UtcNow,
                    TransactionMode = model.TransactionMode,
                    TransactionRef = model.TransactionRef,
                    CreatedBy = model.CreatedBy,
                };
                await _context.TenantUnitTransactions.AddAsync(credit);
                await _context.SaveChangesAsync();
                balance = balance - model.DepositAmount;
            }
            //set new balance
            tenantUnit.CurrentBalance = balance;
            _context.Update(tenantUnit);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"CreateTenantAccount: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<TenantUnitViewDetail> GetTenantAccountDetailsAsync(int tenantUnitId)
    {
        _logger.LogInformation($"GetTenantAccountDetails: {tenantUnitId}");
        try
        {
            var tenantUnitDetails = await (from tu in _context.TenantUnits
                                           join t in _context.Tenants on tu.TenantId equals t.Id
                                           join u in _context.Units on tu.UnitId equals u.Id
                                           join ut in _context.UnitTypes on u.UnitTypeId equals ut.Id
                                           join p in _context.Properties on u.PropertyId equals p.Id
                                           where tu.Id == tenantUnitId
                                           select new TenantUnitViewDetail
                                           {
                                               Id = tu.Id,
                                               AgreedRate = tu.AgreedRate,
                                               StartDate = tu.StartDate,
                                               CurrentBalance = tu.CurrentBalance,
                                               TenantName = t.Name,
                                               TenantMobile = t.MobileNumber,
                                               TenantPhone = t.PhoneNumber,
                                               TenantEmail = t.Email,
                                               UnitNo = u.Name,
                                               UnitDesc = u.Description,
                                               UnitType = ut.Name,
                                               PropertyName = p.Name,
                                               PropertyAddress = p.Address,
                                               PropertyManagerId = p.PropertyManagerId
                                           }).FirstOrDefaultAsync();

            if (tenantUnitDetails == null)
            {
                _logger.LogWarning($"GetTenantAccountDetails: No details found for tenantUnitId {tenantUnitId}");
                throw new BadHttpRequestException($"Failed to fetch details for {tenantUnitId}");
            }
            if (tenantUnitDetails.PropertyManagerId != null)
            {
                var manager = await _userManager.FindByIdAsync(tenantUnitDetails.PropertyManagerId);
                if (manager != null)
                {
                    tenantUnitDetails.PropertyManager = manager.DisplayName;
                    tenantUnitDetails.PropertyManagerPhone = manager.PhoneNumber;
                    tenantUnitDetails.PropertyManagerEmail = manager.Email;
                    tenantUnitDetails.PropertyManagerMobile = manager.MobileNumber;
                }
                else
                {
                    _logger.LogWarning($"GetTenantAccountDetails: No manager found with Id {tenantUnitDetails.PropertyManagerId}");
                }
            }
            //get total debits & credits
            var totalDebits = await _context.TenantUnitTransactions
                .Where(t => t.TenantUnitId == tenantUnitId && t.TransactionType == "D" && t.IsActive)
                .SumAsync(t => (decimal?)t.Amount) ?? 0M;

            var totalCredits = await _context.TenantUnitTransactions
                .Where(t => t.TenantUnitId == tenantUnitId && t.TransactionType == "C" && t.IsActive)
                .SumAsync(t => (decimal?)t.Amount) ?? 0M;
            tenantUnitDetails.TotalBills = totalDebits;
            tenantUnitDetails.TotalPayments = totalCredits;
            return tenantUnitDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetTenantAccountDetails: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<PaginatedList<TenantUnitTransaction>> GetTransactionsAsync(TenantUnitTransactionSearchModel searchModel)
    {
        _logger.LogInformation($"GetTransactionsAsync: {searchModel}");
        try
        {
            if (searchModel == null) throw new BadHttpRequestException("Search Parameters Required");
            IQueryable<TenantUnitTransaction> query = _context.TenantUnitTransactions;
            query = query.Where(q => q.IsActive == searchModel.IsActive && q.TenantUnitId == searchModel.TenantUnitId && q.TransactionDate >= searchModel.FromDate && q.TransactionDate <= searchModel.ToDate);
            var paginatedData = await PaginationHelper.PaginateAsync(query, searchModel.Page, searchModel.PageSize);
            return paginatedData;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetTenantAccountDetails: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<bool> PostTransactionAsync(TenantUnitTransactionCreateModel transactionCreateModel)
    {
        _logger.LogInformation($"PostTransactionAsync: {JsonSerializer.Serialize(transactionCreateModel)}");
        try
        {
            if (transactionCreateModel == null)
                throw new BadHttpRequestException("No Data Submitted");
            var unitAcc = await _context.TenantUnits.Where(t => t.IsActive == true && t.Id == transactionCreateModel.TenantUnitId).FirstOrDefaultAsync();
            if (unitAcc == null)
                throw new BadHttpRequestException($"Tenant Account With Id {transactionCreateModel.TenantUnitId} Not Found");
            var txn = _mapper.Map<TenantUnitTransaction>(transactionCreateModel);
            await _context.TenantUnitTransactions.AddAsync(txn);
            await _context.SaveChangesAsync();
            //update tenant acc balance
            var balance = unitAcc.CurrentBalance;
            if (txn.TransactionType == "C")
                balance = balance - txn.Amount;
            else if (txn.TransactionType == "D")
                balance = balance + txn.Amount;
            unitAcc.CurrentBalance = balance;
            unitAcc.UpdatedBy = txn.CreatedBy;
            unitAcc.UpdatedDate = DateTime.UtcNow;
            _context.TenantUnits.Update(unitAcc);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"PostTransactionAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<bool> UpdateTenantAccountAsync(TenantUnitUpdateModel model)
    {
        _logger.LogInformation($"UpdateTenantAccountAsync: {JsonSerializer.Serialize(model)}");
        try
        {
            if (model == null)
                throw new BadHttpRequestException("No Data Submited");
            var account = await _context.TenantUnits.Where(a => a.Id == model.Id).FirstOrDefaultAsync();
            if (account == null)
                throw new BadHttpRequestException($"Tenant Account With Id {model.Id} Not Found.");
            var unit = await _context.Units.Where(u => u.Id == account.UnitId && u.IsActive == true).FirstOrDefaultAsync();
            if (unit == null)
                throw new BadHttpRequestException($"Unit With Id {account.UnitId} Not Found");
            bool isModified = false;

            if (model.AgreedRate != null && model.AgreedRate != 0 && model.AgreedRate != account.AgreedRate)
            {
                account.AgreedRate = model.AgreedRate;
                isModified = true;
            }

            if (account.IsActive != model.IsActive)
            {
                account.IsActive = model.IsActive;
                isModified = true;

                if (!model.IsActive)
                {
                    unit.IsOccupied = false;
                    _context.Units.Update(unit);
                }
            }

            if (isModified)
            {
                _context.TenantUnits.Update(account);
                await _context.SaveChangesAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"UpdateTenantAccountAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
}