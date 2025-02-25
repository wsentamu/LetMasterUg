using LetMasterWebApp.Core;
using LetMasterWebApp.DataLayer;
using LetMasterWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Text.Json;

namespace LetMasterWebApp.Services;
public interface IReportingServices
{
    public Task<UserStats> GetUserStatsAsync();
    public Task<List<TenantUnitTransactionView>> GetLatestTransactionsAsync();
    public Task<PropertyOccupancy> GetOccupancyAsync();
    public Task<List<PerPropertyOccupancy>> GetPerPropertyOccupancyAsync(string ManagerId);
    public Task<List<Debtor>> GetDebtorsAsync(string propertyManagerId);
    public Task<List<Debtor>> GetDebtsAsync(string userId);
    public Task<List<IncomeExpense>> GetAccountsSummaryAsync(string propertyManagerId);
    public Task<List<TenantUnitTransactionView>> GetLatestTenantTransactionsAsync(string userId);
    public Task<List<TenantAccount>> GetTenantAccountsAsync(string userId);
    public Task<PropertyExpense> GetPropertyExpensesAsync(ReportSearchModel searchModel);
    public Task<AccountStatement> GetAccountTransactionsAsync(ReportSearchModel searchModel);
    public Task<List<SelectListItem>> GetTenantAccountsSelectList(int propertyId);
    public Task<List<SelectListItem>> GetPropertySelectList(string userId);
    public Task<List<SelectListItem>> GetAccountsSelectList(string userId);
    public Task<AccountStatement> GetTenantAccountStatementAsync(ReportSearchModel searchModel);
    public Task<PropertyOccupancyReport> GetOccupancyReportAsync(ReportSearchModel searchModel);
    public Task<PropertyRentTransaction> GetRentTransactionAsync(ReportSearchModel searchModel);
    public Task<PaginatedList<UserMessage>> GetNotificationsAsync(ReportSearchModel searchModel);
}
public class ReportingServices: IReportingServices
{
    private readonly ILogger<ReportingServices> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    public ReportingServices(ILogger<ReportingServices> logger, ApplicationDbContext context, UserManager<User> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }
    //dashboard information
    public async Task<UserStats> GetUserStatsAsync()
    {
        var userStats = new UserStats();

        // Get total number of active users
        userStats.UserCount = await _context.Users.Where(u => u.IsActive).CountAsync();

        // Get count of users per role
        var query = from ur in _context.UserRoles
                    join r in _context.Roles on ur.RoleId equals r.Id
                    join u in _context.Users on ur.UserId equals u.Id
                    where u.IsActive
                    group r by r.Name into g
                    select new UserRolesCount
                    {
                        RoleLabel = g.Key,
                        RoleCount = g.Count()
                    };

        userStats.RolesCount = await query.ToListAsync();
        return userStats;
    }
    //lastest transactions limit 5
    public async Task<List<TenantUnitTransactionView>> GetLatestTransactionsAsync()
    {
        var query = (from t in _context.TenantUnitTransactions
                     join a in _context.TenantUnits on t.TenantUnitId equals a.Id
                     join u in _context.Units on a.UnitId equals u.Id
                     join c in _context.Tenants on a.TenantId equals c.Id
                     join p in _context.Properties on u.PropertyId equals p.Id
                     orderby t.CreatedDate descending

                     select new TenantUnitTransactionView
                     {
                         PropertyName = p.Name,
                         UnitNo = u.Name,
                         TransactionType = t.TransactionType,
                         TransactionDate = t.TransactionDate,
                         Amount = t.Amount??0.0m,
                         Description = t.Description,
                         TenantName = c.Name,
                         DatePosted=t.CreatedDate
                     }).Take(5);
        var transactions = await query.ToListAsync();
        return transactions;
    }
    public async Task<PropertyOccupancy> GetOccupancyAsync()
    {
        var occupancy = new PropertyOccupancy();
        occupancy.TotalProperties = await _context.Properties.Where(p => p.IsActive).CountAsync();
        occupancy.TotalUnits = await _context.Units.Where(u => u.IsActive).CountAsync();
        occupancy.TotalOccupied = await _context.Units.Where(u => u.IsActive && u.IsOccupied).CountAsync();
        var rate = Math.Round((decimal)occupancy.TotalOccupied / (decimal)occupancy.TotalUnits * 100, 2);
        occupancy.OccupancyRate = rate;
        return occupancy;
    }
    //Manager
    public async Task<List<PerPropertyOccupancy>>GetPerPropertyOccupancyAsync(string managerId)
    {
        var query = from p in _context.Properties
                    where p.PropertyManagerId == managerId && p.IsActive == true
                    select new PerPropertyOccupancy
                    {
                        PropertiesName = p.Name,
                        TotalUnits = _context.Units
                                            .Where(u => u.PropertyId == p.Id && u.IsActive == true)
                                            .Count(),
                        TotalOccupied = _context.Units
                                                .Where(u => u.PropertyId == p.Id && u.IsActive == true && u.IsOccupied == true)
                                                .Count(),
                        OccupancyRate = _context.Units
                                                .Where(u => u.PropertyId == p.Id && u.IsActive == true)
                                                .Count() == 0 ? 0 :
                                            ((decimal)_context.Units
                                                .Where(u => u.PropertyId == p.Id && u.IsActive == true && u.IsOccupied == true)
                                                .Count() / _context.Units
                                                .Where(u => u.PropertyId == p.Id && u.IsActive == true)
                                                .Count()) * 100
                    };

        var result = await query.ToListAsync();
        foreach (var item in result)
        {
            if(item.OccupancyRate!=null)
                item.OccupancyRate = Math.Round((decimal)item.OccupancyRate, 2);
        }
        return result;
    }
    public async Task<List<Debtor>>GetDebtorsAsync(string propertyManagerId)
    {
        var query = from a in _context.TenantUnits
                    join t in _context.Tenants on a.TenantId equals t.Id
                    join u in _context.Units on a.UnitId equals u.Id
                    join p in _context.Properties on u.PropertyId equals p.Id
                    where a.IsActive == true && p.PropertyManagerId == propertyManagerId
                    orderby a.CurrentBalance descending
                    select new Debtor
                    {
                        TenantName = t.Name,
                        Amount = a.CurrentBalance??0.0m,
                        UnitNo = u.Name,
                        AgreedRate = a.AgreedRate,
                        PropertyName = p.Name,
                        DateLastPaid = (from txn in _context.TenantUnitTransactions
                                        where txn.TenantUnitId == a.Id
                                        orderby txn.TransactionDate descending
                                        select txn.TransactionDate).FirstOrDefault(),
                        AmountLastPaid = (from txn in _context.TenantUnitTransactions
                                          where txn.TenantUnitId == a.Id
                                          orderby txn.TransactionDate descending
                                          select txn.Amount).FirstOrDefault()
                    };

        var results = await query.Take(5).ToListAsync();
        return results;
    }
    public async Task<List<IncomeExpense>>GetAccountsSummaryAsync(string propertyManagerId)
    {
        var cutoffDate = DateTime.Now.AddMonths(-6);

        var query = from p in _context.Properties
                    where p.PropertyManagerId == propertyManagerId && p.IsActive == true
                    join u in _context.Units.Where(u => u.IsActive == true) on p.Id equals u.PropertyId into unitsGroup
                    from u in unitsGroup.DefaultIfEmpty()
                    join a in _context.TenantUnits on u.Id equals a.UnitId into tenantUnitsGroup
                    from a in tenantUnitsGroup.DefaultIfEmpty()
                    join t in _context.TenantUnitTransactions.Where(t => t.IsActive == true && t.TransactionDate >= cutoffDate) on a.Id equals t.TenantUnitId into transactionsGroup
                    from t in transactionsGroup.DefaultIfEmpty()
                    group new { p, t } by new { p.Id, p.Name } into g
                    select new IncomeExpense
                    {
                        PropertyId = g.Key.Id,
                        PropertyName = g.Key.Name,
                        TotalIncomeRecieved = g.Where(x => x.t != null && x.t.TransactionType == "C").Sum(x => (decimal?)x.t.Amount) ?? 0,
                        TotalRentRecievable = g.Where(x => x.t != null && x.t.TransactionType == "D").Sum(x => (decimal?)x.t.Amount) ?? 0,
                        TotalExpenses = (from e in _context.Expenses
                                         where e.PropertyId == g.Key.Id && e.IsActive == true && e.TransactionDate >= cutoffDate
                                         select (decimal?)e.Amount).Sum() ?? 0
                    };

        var result = await query.ToListAsync();
        return result;
    }
    //Tenant
    public async Task<List<Debtor>> GetDebtsAsync(string userId)
    {
        var query = from a in _context.TenantUnits
                    join t in _context.Tenants on a.TenantId equals t.Id
                    join u in _context.Units on a.UnitId equals u.Id
                    join p in _context.Properties on u.PropertyId equals p.Id
                    where a.IsActive == true && t.UserId==userId
                    orderby a.CurrentBalance descending
                    select new Debtor
                    {
                        TenantName = t.Name,
                        Amount = a.CurrentBalance??0.0m,
                        UnitNo = u.Name,
                        AgreedRate = a.AgreedRate,
                        PropertyName = p.Name,
                        DateLastPaid = (from txn in _context.TenantUnitTransactions
                                        where txn.TenantUnitId == a.Id
                                        orderby txn.TransactionDate descending
                                        select txn.TransactionDate).FirstOrDefault(),
                        AmountLastPaid = (from txn in _context.TenantUnitTransactions
                                          where txn.TenantUnitId == a.Id
                                          orderby txn.TransactionDate descending
                                          select txn.Amount).FirstOrDefault()
                    };

        var results = await query.Take(5).ToListAsync();
        return results;
    }
    public async Task<List<TenantUnitTransactionView>> GetLatestTenantTransactionsAsync(string userId)
    {
        var query = (from t in _context.TenantUnitTransactions
                     join a in _context.TenantUnits on t.TenantUnitId equals a.Id
                     join u in _context.Units on a.UnitId equals u.Id
                     join c in _context.Tenants on a.TenantId equals c.Id
                     join p in _context.Properties on u.PropertyId equals p.Id
                     where c.UserId == userId
                     orderby t.CreatedDate descending

                     select new TenantUnitTransactionView
                     {
                         PropertyName = p.Name,
                         UnitNo = u.Name,
                         TransactionType = t.TransactionType,
                         TransactionDate = t.TransactionDate,
                         Amount = t.Amount??0.0m,
                         Description = t.Description,
                         TenantName = c.Name,
                         DatePosted = t.CreatedDate
                     }).Take(5);
        var transactions = await query.ToListAsync();
        return transactions;
    }
    public async Task<List<TenantAccount>>GetTenantAccountsAsync(string userId)
    {
        var query=(from a in _context.TenantUnits
                   join u in _context.Units on a.UnitId equals u.Id
                   join c in _context.Tenants on a.TenantId equals c.Id
                   join d in _context.Users on c.UserId equals d.Id
                   join p in _context.Properties on u.PropertyId equals p.Id
                   join m in _context.Users on p.PropertyManagerId equals m.Id
                   where c.UserId==userId
                   select new TenantAccount
                   {
                       AgreedRate=a.AgreedRate,
                       CurrentBalance=a.CurrentBalance,
                       Id=a.Id,
                       PropertyManager=m.DisplayName,
                       PropertyName=p.Name,
                       UnitName=u.Name
                   }).Take(5);
        var accounts=await query.ToListAsync();
        return accounts;
    }
    //reports
    public async Task<PropertyOccupancyReport> GetOccupancyReportAsync(ReportSearchModel searchModel)
    {
        try
        {
            _logger.LogInformation($"GetOccupancyReportAsync: {JsonSerializer.Serialize(searchModel)}");
            if (searchModel == null)
                throw new BadHttpRequestException("No Search Parameters");
            var occupancy = await (from p in _context.Properties
                                   join t in _context.PropertyTypes on p.PropertyTypeId equals t.Id
                                   where p.Id==searchModel.PropertyId
                                   select new PropertyOccupancyReport
                                   {
                                       PropertyName=p.Name,
                                       PropertyDesc=p.Description,
                                       PropertyAddress = p.Address,
                                       PropertyType=t.Name,
                                   }).FirstOrDefaultAsync();
            if (occupancy == null)
                throw new BadHttpRequestException($"Property with Id {searchModel.PropertyId} not found.");
            occupancy.TotalUnits= await _context.Units.Where(u=>u.PropertyId==searchModel.PropertyId && u.IsActive).CountAsync();
            occupancy.OccupiedUnits = await _context.Units.Where(u => u.PropertyId == searchModel.PropertyId && u.IsActive && u.IsOccupied).CountAsync();
            var query = from u in _context.Units
                        join t in _context.UnitTypes on u.UnitTypeId equals t.Id
                        join a in _context.TenantUnits on u.Id equals a.UnitId into unitTenantJoin
                        from a in unitTenantJoin.DefaultIfEmpty()
                        join c in _context.Tenants on a.TenantId equals c.Id into tenantJoin
                        from c in tenantJoin.DefaultIfEmpty()
                        where u.IsActive == true && u.PropertyId == searchModel.PropertyId
                        select new UnitDetailsViewModel
                        {
                            UnitNo = u.Name,
                            UnitType = t.Name,
                            UnitDesc = u.Description,
                            IsOccupied = a != null && a.IsActive,
                            StandardRate = u.StandardRate,
                            TenantName = a != null && a.IsActive ? c.Name : null,
                            TenantContact = a != null && a.IsActive ? $"{c.PhoneNumber} {c.MobileNumber}" : null,
                            TenantEmail = a != null && a.IsActive ? c.Email : null,
                            StartDate = a != null && a.IsActive ? a.StartDate : (DateTime?)null,
                            AgreedRate = a != null && a.IsActive ? a.AgreedRate : (decimal?)null,
                            CurrentBalance = a != null && a.IsActive ? a.CurrentBalance : (decimal?)null,
                        };
            occupancy.UnitDetails=await query.ToListAsync();
            return occupancy;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetOccupancyReportAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<PropertyRentTransaction>GetRentTransactionAsync(ReportSearchModel searchModel)
    {
        try
        {
            _logger.LogInformation($"GetRentTransactionAsync: {JsonSerializer.Serialize(searchModel)}");
            if (searchModel == null)
                throw new BadHttpRequestException("No Search Parameters");
            var rentTransaction = await _context.Properties.Where(p => p.Id == searchModel.PropertyId).Select(p => new PropertyRentTransaction
            {
                PropertyAddress = p.Address,
                PropertyName = p.Name,
            }).FirstOrDefaultAsync();
            if (rentTransaction == null)
                throw new BadHttpRequestException("Property not found");
            var query = (from t in _context.TenantUnitTransactions
                         join a in _context.TenantUnits on t.TenantUnitId equals a.Id
                         join c in _context.Tenants on a.TenantId equals c.Id
                         join u in _context.Units on a.UnitId equals u.Id
                         join p in _context.Properties on u.PropertyId equals p.Id
                         where p.Id == searchModel.PropertyId && t.TransactionDate >= searchModel.DateFrom && t.TransactionDate <= searchModel.DateTo
                         orderby t.TransactionDate
                         select new PropertyRentTransactionEntry
                         {
                             AccountId = a.Id,
                             UnitId = u.Id,
                             TransactionId = t.Id,
                             TenantName = c.Name,
                             UnitNo = u.Name,
                             Amount = t.Amount,
                             TransactionCreationDate = t.CreatedDate,
                             TransactionDate = t.TransactionDate,
                             TransactionDesc = t.Description,
                             TransactionMode = t.TransactionMode,
                             TransactionRef = t.TransactionRef,
                             TransactionType = t.TransactionType,
                         });
            rentTransaction.TransactionEntries = await query.ToListAsync();
            return rentTransaction;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetRentTransactionAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<PropertyExpense> GetPropertyExpensesAsync(ReportSearchModel searchModel)
    {
        //required fields propertyId, dateFrom, dateTo
        try
        {
            _logger.LogInformation($"GetPropertyExpensesAsync: {JsonSerializer.Serialize(searchModel)}");
            if (searchModel == null)
                throw new BadHttpRequestException("No Search Parameters");
            var expense=await _context.Properties.Where(p => p.Id == searchModel.PropertyId).Select(p=>new PropertyExpense
            {
                PropertyAddress=p.Address,
                PropertyId=p.Id,
                PropertyName=p.Name,
            }).FirstOrDefaultAsync();
            if(expense== null)
                throw new BadHttpRequestException($"Could not find property");
            var query = (from e in _context.Expenses
                         join p in _context.Properties on e.PropertyId equals p.Id
                         join t in _context.ExpenseTypes on e.ExpenseTypeId equals t.Id
                         where e.TransactionDate >= searchModel.DateFrom && e.TransactionDate <= searchModel.DateTo && e.PropertyId == searchModel.PropertyId orderby e.TransactionDate
                         select new PropertyExpenseEntry
                         {
                             Amount = e.Amount,
                             ExpenseDesc = e.Description,
                             ExpenseId = e.Id,
                             ExpenseType = t.Name,
                             TransactionDate = e.TransactionDate,
                             TransactionCreationDate = e.CreatedDate,
                             UnitId = e.UnitId,
                         });
            expense.Expenses = await query.ToListAsync();
            foreach (var expenseItem in expense.Expenses)
            {
                if (expenseItem.UnitId != null)
                    expenseItem.UnitNo = await _context.Units.Where(u => u.Id == expenseItem.UnitId).Select(u => u.Name).FirstOrDefaultAsync();
            }
            _logger.LogInformation($"GetPropertyExpensesAsync: {expense.Expenses.Count.ToString()} Results Returned");
            return expense;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetPropertyExpensesAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<AccountStatement> GetAccountTransactionsAsync(ReportSearchModel searchModel)
    {
        try
        {
            _logger.LogInformation($"GetAccountTransactionsAsync: {JsonSerializer.Serialize(searchModel)}");
            if (searchModel == null)
                throw new BadHttpRequestException("No Search Parameters");
            var statement = await (from a in _context.TenantUnits
                              join c in _context.Tenants on a.TenantId equals c.Id
                              join u in _context.Units on a.UnitId equals u.Id
                              join p in _context.Properties on u.PropertyId equals p.Id
                              join m in _context.Users on p.PropertyManagerId equals m.Id
                              where a.Id==searchModel.TenantAccountId
                              select new AccountStatement
                              {
                                  AccountId=a.Id,
                                  ContactEmail=m.Email,
                                  ContactNumber=$"{m.PhoneNumber} {m.MobileNumber}",
                                  PropertyManager=m.DisplayName,
                                  PropertyName=p.Name,
                                  Tenant=c.Name,
                                  TenantContact=$"{c.PhoneNumber} {c.MobileNumber}",
                                  TenantEmail=c.Email,
                                  PropertyAddress=p.Address,
                                  UnitId = u.Id,
                                  UnitNo = u.Name,
                                  AgreedRate=a.AgreedRate,
                                  CurrentBalance=a.CurrentBalance,
                              }).FirstOrDefaultAsync();
            if (statement == null)
                throw new BadHttpRequestException("Account not found");
            var query = (from t in _context.TenantUnitTransactions
                               join a in _context.TenantUnits on t.TenantUnitId equals a.Id
                               join c in _context.Tenants on a.TenantId equals c.Id
                               join u in _context.Units on a.UnitId equals u.Id
                               join p in _context.Properties on u.PropertyId equals p.Id
                         where c.UserId == searchModel.UserId && a.Id==searchModel.TenantAccountId && t.TransactionDate>=searchModel.DateFrom && t.TransactionDate<=searchModel.DateTo
                               select new UnitAccountTransaction
                               {
                                   Amount=t.Amount,
                                   TransactionCreationDate=t.CreatedDate,
                                   TransactionDate=t.TransactionDate,
                                   TransactionDesc=t.Description,
                                   TransactionId=t.Id,
                                   TransactionMode=t.TransactionMode,
                                   TransactionRef=t.TransactionRef,
                                   TransactionType = t.TransactionType
                               });
            if(!string.IsNullOrEmpty(searchModel.TransactionType))
                query=query.Where(t=>t.TransactionType==searchModel.TransactionType);
            statement.AccountTransactions = await query.ToListAsync();
            return statement;
        }
        catch(Exception ex)
        {
            _logger.LogError($"GetAccountTransactionsAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<AccountStatement> GetTenantAccountStatementAsync(ReportSearchModel searchModel)
    {
        try
        {
            _logger.LogInformation($"GetTenantAccountStatementAsync: {JsonSerializer.Serialize(searchModel)}");
            if (searchModel == null)
                throw new BadHttpRequestException("No Search Parameters");
            var statement = await (from a in _context.TenantUnits
                                   join c in _context.Tenants on a.TenantId equals c.Id
                                   join u in _context.Units on a.UnitId equals u.Id
                                   join p in _context.Properties on u.PropertyId equals p.Id
                                   join m in _context.Users on p.PropertyManagerId equals m.Id
                                   where a.Id == searchModel.TenantAccountId
                                   select new AccountStatement
                                   {
                                       AccountId = a.Id,
                                       ContactEmail = m.Email,
                                       ContactNumber = $"{m.PhoneNumber} {m.MobileNumber}",
                                       PropertyManager = m.DisplayName,
                                       PropertyName = p.Name,
                                       Tenant = c.Name,
                                       TenantContact = $"{c.PhoneNumber} {c.MobileNumber}",
                                       TenantEmail = c.Email,
                                       PropertyAddress = p.Address,
                                       UnitId = u.Id,
                                       UnitNo = u.Name,
                                       AgreedRate = a.AgreedRate,
                                       CurrentBalance = a.CurrentBalance,
                                   }).FirstOrDefaultAsync();
            if (statement == null)
                throw new BadHttpRequestException("Account not found");
            var query = (from t in _context.TenantUnitTransactions
                         join a in _context.TenantUnits on t.TenantUnitId equals a.Id
                         join c in _context.Tenants on a.TenantId equals c.Id
                         join u in _context.Units on a.UnitId equals u.Id
                         join p in _context.Properties on u.PropertyId equals p.Id
                         where a.Id == searchModel.TenantAccountId && t.TransactionDate>=searchModel.DateFrom && t.TransactionDate<=searchModel.DateTo
                         select new UnitAccountTransaction
                         {
                             Amount = t.Amount,
                             TransactionCreationDate = t.CreatedDate,
                             TransactionDate = t.TransactionDate,
                             TransactionDesc = t.Description,
                             TransactionId = t.Id,
                             TransactionMode = t.TransactionMode,
                             TransactionRef = t.TransactionRef,
                             TransactionType = t.TransactionType
                         });
            if (!string.IsNullOrEmpty(searchModel.TransactionType))
                query = query.Where(t => t.TransactionType == searchModel.TransactionType);
            statement.AccountTransactions = await query.ToListAsync();
            return statement;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetTenantAccountStatementAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<PaginatedList<UserMessage>>GetNotificationsAsync(ReportSearchModel searchModel)
    {
        _logger.LogInformation($"GetNotificationsAsync: {JsonSerializer.Serialize(searchModel)}");
        try
        {
            if (searchModel == null) throw new BadHttpRequestException("Search Parameters Required");
            IQueryable<UserMessage> query = _context.UserMessages;
            query = query.Where(q => q.CreatedDate>=searchModel.DateFrom && q.CreatedDate<=searchModel.DateTo).OrderByDescending(n=>n.CreatedDate);
            if (searchModel.Email != null)
                query = query.Where(q => q.MessageReciepient.ToUpper().Contains(searchModel.Email));
            if (searchModel.Mobile != null)
                query = query.Where(q => q.MessageReciepient.ToUpper().Contains(searchModel.Mobile));
            if (searchModel.Subject != null)
                query = query.Where(q => q.MessageSubject!.ToUpper().Contains(searchModel.Subject));
            return await PaginationHelper.PaginateAsync(query, searchModel.Page, searchModel.PageSize);
        }
        catch(Exception ex)
        {
            _logger.LogError($"GetNotificationsAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    //Select List Generation Methods
    public async Task<List<SelectListItem>> GetTenantAccountsSelectList(int propertyId)
    {
        try
        {
            var tenants = await (from a in _context.TenantUnits
                           join u in _context.Units on a.UnitId equals u.Id
                           join t in _context.Tenants on a.TenantId equals t.Id
                           where u.PropertyId == propertyId
                           select new SelectListItem { 
                               Value=a.Id.ToString(),
                               Text=$"{t.Name} ({u.Name})"
                           }).ToListAsync();
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetTenantAccountsAsync: {propertyId} exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
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
    public async Task<List<SelectListItem>> GetAccountsSelectList(string userId)
    {
        try
        {
            var tenants = await (from a in _context.TenantUnits
                                 join u in _context.Units on a.UnitId equals u.Id
                                 join t in _context.Tenants on a.TenantId equals t.Id
                                 join p in _context.Properties on u.PropertyId equals p.Id
                                 where t.UserId == userId && a.IsActive==true
                                 select new SelectListItem
                                 {
                                     Value = a.Id.ToString(),
                                     Text = $"{p.Name} ({u.Name})"
                                 }).ToListAsync();
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetAccountsSelectList: {userId} exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
}
