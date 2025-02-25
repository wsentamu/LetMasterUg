using Hangfire;
using LetMasterWebApp.DataLayer;
using LetMasterWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LetMasterWebApp.Services;
public class TenantBillingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantBillingService> _logger;
    private readonly IConfiguration _configuration;
    public TenantBillingService(IServiceProvider serviceProvider, ILogger<TenantBillingService> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Tenant Billing Service is starting at {DateTime.Now}.");
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            DateTime nextRun;

            using (var scope = _serviceProvider.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var job = await _context.ScheduledJobs.Where(b => b.JobName == "MonthlyBilling").FirstOrDefaultAsync();
                nextRun = job!.NextRunTime;
            }
            TimeSpan delay = nextRun - now;
            if (delay < TimeSpan.Zero)
                delay = TimeSpan.Zero;
            _logger.LogInformation($"Next bill generation scheduled for {nextRun} (UTC).");
            try
            {
                await Task.Delay(delay, stoppingToken);
                if (now.Day == 1)
                    await GenerateAndSaveBillsAsync();
                else if (now.Day == 20)
                    await SendArrearsReminderAsync();
                using (var scope = _serviceProvider.CreateScope())
                {
                    var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var job = await _context.ScheduledJobs.Where(b => b.JobName == "MonthlyBilling").FirstOrDefaultAsync();
                    job!.NextRunTime = GetNextRunTime(nextRun);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating bills.");
            }
            now = DateTime.UtcNow;
            nextRun = GetNextRunTime(now);
            delay = nextRun - now;

            await Task.Delay(delay, stoppingToken);
        }
    }
    private async Task GenerateAndSaveBillsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var accounts = await _context.TenantUnits.Where(a => a.IsActive == true).ToListAsync();
            _logger.LogInformation($"Starting billing. {accounts.Count} active accounts found.");
            var bills = new List<TenantUnitTransaction>();
            var Today = DateTime.Now;
            var billCount = 0;
            foreach (var account in accounts)
            {
                var balance = account.CurrentBalance;
                var rate = account.AgreedRate;

                // Update the account balance
                account.CurrentBalance = balance + rate;

                // Create a new bill object
                var bill = new TenantUnitTransaction
                {
                    TenantUnitId = account.Id,
                    Amount = rate,
                    IsActive = account.IsActive,
                    TransactionType = "D",
                    Description = $"BILL {Today.Year}-{Today.Month}",
                    TransactionMode = "SYSTEM",
                    TransactionRef = $"D{account.Id}-{Today.Year}{Today.Month}",
                    CreatedDate = Today,
                    TransactionDate = Today
                };
                // Add the bill to the list of bills to be saved
                billCount++;
                bills.Add(bill);
            }
            // Save the updated accounts and new bills to the database
            await _context.TenantUnitTransactions.AddRangeAsync(bills);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"{billCount} of {accounts.Count} billed for period {Today.Month} {Today.Year}");
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error occurred during the bill generation transaction.");
            throw;
        }
    }
    private DateTime GetNextRunTime(DateTime currentTime)
    {
        var currentDay = currentTime.Day;
        if (currentDay < 1 || currentDay > 20)
        {
            var nextMonth = currentTime.AddMonths(1);
            return new DateTime(nextMonth.Year, nextMonth.Month, 1);
        }
        else if (currentDay < 20)
        {
            return new DateTime(currentTime.Year, currentTime.Month, 20);
        }
        else
        {
            var nextMonth = currentTime.AddMonths(1);
            return new DateTime(nextMonth.Year, nextMonth.Month, 1);
        }
    }
    //send out arrears notices where current bal > agreed rent is due via email + SMS if user has email + phone/mobile
    private async Task SendArrearsReminderAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var _notification = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var _background = scope.ServiceProvider.GetRequiredService<BackgroundJobClient>();
        try
        {
            var tenantList = await _context.Tenants
                .Where(t => _context.TenantUnits.Any(ta => ta.IsActive && ta.CurrentBalance > ta.AgreedRate)).Distinct()
            .ToListAsync();
            var emailTemplate = _configuration.GetValue<string>("NotificationTemplates:ArrearsEmailTemplate");
            var footerTemplate = _configuration.GetValue<string>("NotificationTemplates:FooterTemplate");
            var smsTemplate = _configuration.GetValue<string>("NotificationTemplates:ArrearsSmsTemplate");
            bool sendMail = false;
            bool sendSms = false;
            if (!string.IsNullOrEmpty(emailTemplate))
                sendMail = true;
            if (!string.IsNullOrEmpty(smsTemplate))
                sendSms = true;
            if (!sendMail && !sendSms)
                return;
            foreach (var tenant in tenantList)
            {
                var accounts = await (from ta in _context.TenantUnits
                                      join u in _context.Units on ta.UnitId equals u.Id
                                      join p in _context.Properties on u.PropertyId equals p.Id
                                      where ta.IsActive && ta.TenantId == tenant.Id && ta.CurrentBalance > ta.AgreedRate
                                      select new TenantUnitViewModel
                                      {
                                          CurrentBalance = ta.CurrentBalance ?? 0,
                                          PropertyName = p.Name,
                                          UnitName = u.Name,
                                      }).ToListAsync();
                if (accounts.Count > 0)
                {
                    var smsAcc = string.Empty;
                    var emailAcc = string.Empty;
                    foreach (var account in accounts)
                    {
                        smsAcc += $"{account.PropertyName}({account.UnitName}): {account.CurrentBalance}, ";
                        emailAcc += $"<p>{account.PropertyName}({account.UnitName}): {account.CurrentBalance}</p>";
                    }
                    if (smsAcc.Length > 0)
                        smsAcc = smsAcc.TrimEnd(' ', ',').ToString();
                    if (sendSms && (!string.IsNullOrEmpty(tenant.PhoneNumber) || !string.IsNullOrEmpty(tenant.MobileNumber)))
                    {
                        var smsBody = smsTemplate!.Replace("{FULLNAME}", tenant.Name).Replace("{SUMMARY}", smsAcc);
                        var reciever = tenant.MobileNumber;
                        if (string.IsNullOrEmpty(reciever))
                            reciever = tenant.PhoneNumber;
                        _background.Enqueue(()=> _notification.SendSms(reciever!, smsBody));
                    }
                    if (sendMail && !string.IsNullOrEmpty(tenant.Email))
                    {
                        var emailSubject = "Rent Account Arrears Reminder";
                        var emailBody = emailTemplate!.Replace("{FULLNAME}", tenant.Name).Replace("{ACCOUNTS}", emailAcc) + footerTemplate;
                        _background.Enqueue(() => _notification.SendEmailAsync(tenant.Email, emailSubject, emailBody));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during arrears notification");
        }
    }
}
