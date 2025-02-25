using Hangfire;
using Hangfire.MySql;
using LetMasterWebApp.Core;
using LetMasterWebApp.DataLayer;
using LetMasterWebApp.Models;
using LetMasterWebApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using Serilog;
using Serilog.Debugging;

string? environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();
SelfLog.Enable(Console.WriteLine);
var loggerConfig = new LoggerConfiguration()
    .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .MinimumLevel.Information();
Log.Logger = loggerConfig.CreateLogger();
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});
builder.Services.AddIdentity<User, IdentityRole>(option => option.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddHangfire(config => config.UseStorage(new MySqlStorage(connectionString, new MySqlStorageOptions
{
    TablesPrefix = "Hangfire"
})));
builder.Services.AddHangfireServer();

//builder.Services.ConfigureApplicationCookie(c =>{ 
//    c.LoginPath = "/login";
//    c.AccessDeniedPath = "/access-denied";
//});

//builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();
//dependency injection
var assemblies = new[] { typeof(Program).Assembly };
builder.ConfigureAutoMapper(assemblies);
builder.Services.ConfigureDependencyInjection(assemblies);
builder.Services.AddAutoMapper(typeof(Program));
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddRazorPages().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Host.UseSerilog();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var userServices = services.GetRequiredService<IUserServices>();
        await userServices.SeedRoles();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding roles.");
    }
}
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseHangfireDashboard();
RecurringJob.AddOrUpdate<IPaymentService>("DebitRequestProcessing", service => service.ProcessPendingAsync(), Cron.Minutely);
app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Let Master Ug API v1");
});
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

try
{
    Log.Information("boot.start LetMasterWebApp in  {Environment} & " + connectionString, environmentName);
    app.Run();
    Log.Information("boot.success LetMasterWebApp {Environment}", environmentName);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Log.Error(ex, "boot.error terminated unexpectedly {Environment}", environmentName);
}
finally
{
    Log.CloseAndFlush();
}