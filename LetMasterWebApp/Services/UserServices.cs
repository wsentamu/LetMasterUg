using AutoMapper;
using Hangfire;
using LetMasterWebApp.Core;
using LetMasterWebApp.DataLayer;
using LetMasterWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
namespace LetMasterWebApp.Services;
public interface IUserServices
{
    public Task<bool> SeedRoles();
    public Task<List<SelectListItem>> GetAllRoles();
    public Task<PaginatedList<UserViewModel>> ListUsers(UserSearchModel searchModel);
    public Task<UserViewModel> GetUser(string id);
    public Task<Serviceresponse> CreateUser(UserCreateModel model);
    public Task<bool> EditUser(UserUpdateModel model);
    public Task<SignInResult> CustomPasswordSignInAsync(string username, string password, bool rememberMe, bool lookOut);
}
public class UserServices : IUserServices
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UserServices> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly SignInManager<User> _signInManager;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IConfiguration _config;
    private readonly LinkGenerator _linkGenerator;
    private readonly INotificationService _notificationService;
    private readonly IBackgroundJobClient _jobClient;
    public UserServices(RoleManager<IdentityRole> roleManager, ILogger<UserServices> logger, ApplicationDbContext context, IMapper mapper, UserManager<User> userManager, SignInManager<User> signInManager, IHttpContextAccessor contextAccessor, IConfiguration config, LinkGenerator linkGenerator, INotificationService notificationService, IBackgroundJobClient jobClient)
    {
        _roleManager = roleManager;
        _logger = logger;
        _context = context;
        _mapper = mapper;
        _userManager = userManager;
        _signInManager = signInManager;
        _contextAccessor = contextAccessor;
        _config = config;
        _linkGenerator = linkGenerator;
        _notificationService = notificationService;
        _jobClient = jobClient;
    }
    public async Task<bool> SeedRoles()
    {
        var roles = new List<string> { "Admin", "Manager", "Tenant" };
        bool success = true;
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    success = false;
                    _logger.LogError($"Error creating role {role}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
        return success;
    }
    //custom login
    public async Task<SignInResult> CustomPasswordSignInAsync(string username, string password, bool rememberMe, bool lookOut)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
            return SignInResult.Failed;
        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lookOut);
        if (result.Succeeded)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
                {
                    new Claim("FullName", user.DisplayName ?? string.Empty),
                };
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            //DB claims as opposed to cookies that may lose the data
            var existingClaims = await _userManager.GetClaimsAsync(user);
            var fullNameClaim = existingClaims.FirstOrDefault(c => c.Type == "FullName");
            if (fullNameClaim != null)
                await _userManager.RemoveClaimAsync(user, fullNameClaim);
            await _userManager.AddClaimAsync(user, new Claim("FullName", user.DisplayName ?? "User"));
            await _signInManager.SignInWithClaimsAsync(user, rememberMe, claims);
        }
        return result;
    }
    //update user

    //list users with search model
    public async Task<PaginatedList<UserViewModel>> ListUsers(UserSearchModel searchModel)
    {
        _logger.LogInformation($"ListUsers: {JsonSerializer.Serialize(searchModel)}");
        try
        {
            IQueryable<User> query = _context.Users;
            if (searchModel == null)
                throw new BadHttpRequestException("Search Parameters Required");
            query = query.Where(q => q.IsActive == searchModel.IsActive);
            if (!string.IsNullOrWhiteSpace(searchModel.UserName))
                query = query.Where(q => q.UserName == searchModel.UserName);
            if (!string.IsNullOrWhiteSpace(searchModel.Email))
                query = query.Where(query => query.Email == searchModel.Email);
            if (!string.IsNullOrWhiteSpace(searchModel.PhoneNumber))
                query = query.Where(q => q.PhoneNumber == searchModel.PhoneNumber || q.MobileNumber == searchModel.PhoneNumber);
            if (!string.IsNullOrWhiteSpace(searchModel.MobileNumber))
                query = query.Where(q => q.PhoneNumber == searchModel.MobileNumber || q.MobileNumber == searchModel.MobileNumber);
            if (!string.IsNullOrWhiteSpace(searchModel.DisplayName))
                query = query.Where(q => q.DisplayName!.Contains(searchModel.DisplayName));
            if (!string.IsNullOrWhiteSpace(searchModel.Role))
            {
                query = from user in query
                        join userRole in _context.UserRoles on user.Id equals userRole.UserId
                        join role in _context.Roles on userRole.RoleId equals role.Id
                        where role.Name == searchModel.Role
                        select user;
            }
            var paginatedData = await PaginationHelper.PaginateAsync(query, searchModel.Page, searchModel.PageSize);
            var users = await query.ToListAsync();
            var finalList = new List<UserViewModel>();
            foreach (var user in paginatedData.Items)
            {
                var userView = _mapper.Map<UserViewModel>(user);
                var roles = await _userManager.GetRolesAsync(user);
                userView.Roles = roles.ToList();
                finalList.Add(userView);
            }
            var paginatedList = new PaginatedList<UserViewModel>(finalList, paginatedData.TotalCount, paginatedData.CurrentPage, paginatedData.PageSize);
            return paginatedList;
        }
        catch (Exception ex)
        {
            _logger.LogError($"ListUsers: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<List<SelectListItem>> GetAllRoles()
    {
        try
        {
            var roles = await _roleManager.Roles.Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Name
            }).ToListAsync();
            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetAllRoles: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<UserViewModel> GetUser(string id)
    {
        _logger.LogInformation($"GetUser: {id}");
        try
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                _logger.LogWarning($"GetUser: User with ID {id} not found.");
                throw new KeyNotFoundException($"User with Id {id} not found");
            }
            var roles = await _userManager.GetRolesAsync(user);
            var userView = _mapper.Map<UserViewModel>(user);
            userView.Roles = roles.ToList();
            return userView;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetUser: exception {ex.Message}", ex);
            throw new BadHttpRequestException($"Error fetching user with ID {id}.", ex);
        }
    }
    //create user
    public async Task<Serviceresponse> CreateUser(UserCreateModel model)
    {
        _logger.LogInformation($"CreateUser: {JsonSerializer.Serialize(model)}");
        try
        {
            var response = new Serviceresponse { Success = false };
            if (model == null)
            {
                response.ErrorMsg = "No data submitted";
                return response;
            }
            if (model.OtherNames == null && model.FirstName == null && model.LastName == null)
            {
                response.ErrorMsg = "Name is required";
                return response;
            }
            //generate display name based on user type
            if (model.IsIndividual)
                model.DisplayName = $"{model.FirstName} {model.LastName}";
            else
                model.DisplayName = model.OtherNames;
            //validations
            var exist = await _userManager.FindByNameAsync(model.Email);
            if (exist != null)
            {
                response.ErrorMsg = $"User with username {model.Email} already exists";
                return response;
            }
            var user = _mapper.Map<User>(model);
            user.UserName = model.Email;
            //generate random password and store & later send to user
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
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                response.ErrorMsg = "User creation failed";
                return response;
            }
            foreach (var role in model.Roles!)
            {
                var roleRes = await _userManager.AddToRoleAsync(user, role);
                if (!roleRes.Succeeded)
                    _logger.LogError($"Failed to Add {user.UserName} to role {role}");
            }
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
            var body = _config.GetValue<string>("NotificationTemplates:WelcomeEmailTemplate");

            if (!string.IsNullOrEmpty(body))
                body = body.Replace("{FULLNAME}", user.DisplayName).Replace("{LINK}", indexUrl).Replace("{PASS}", password);
            var footer = _config.GetValue<string>("NotificationTemplates:FooterTemplate");
            if (!string.IsNullOrEmpty(footer))
                footer = footer.Replace("{YEAR}", DateTime.Now.Year.ToString());
            body += footer;
            _logger.LogInformation(body);
            _jobClient.Enqueue(() => _notificationService.SendEmailAsync(model.Email, subject, body));
            var smsBody = _config.GetValue<string>("NotificationTemplates:WelcomeSmsTemplate");
            var mobile = model.MobileNumber;
            if (string.IsNullOrEmpty(mobile))
                mobile = model.PhoneNumber;
            if ((!string.IsNullOrEmpty(model.MobileNumber) || !string.IsNullOrEmpty(model.PhoneNumber)) && !string.IsNullOrEmpty(smsBody) && !string.IsNullOrEmpty(mobile))
            {
                smsBody = smsBody.Replace("{FULLNAME}", user.DisplayName).Replace("{EMAIL}", user.Email);
                _jobClient.Enqueue(() => _notificationService.SendSms(mobile, smsBody));
            }
            response.Success = true;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetUser: exception {ex.Message}", ex);
            throw new BadHttpRequestException($"Error creating user: {ex}", ex);
        }
    }
    public async Task<bool> EditUser(UserUpdateModel model)
    {
        _logger.LogInformation($"Edit User: {JsonSerializer.Serialize(model)}");
        try
        {
            if (model == null || model.Id == null)
                throw new ArgumentNullException($"Mandatory field missing");
            var original = await _userManager.FindByIdAsync(model.Id);
            if (original == null)
                throw new KeyNotFoundException($"User with Id {model.Id} not found");
            original.IsActive = model.IsActive;
            if (model.IsIndividual)
            {
                original.OtherNames = null;
                if (model.FirstNameUpdate != null)
                    original.FirstName = model.FirstNameUpdate;
                if (model.LastNameUpdate != null)
                    original.LastName = model.LastNameUpdate;
                original.DisplayName = $"{original.FirstName} {original.LastName}";
            }
            else
            {
                original.FirstName = null;
                original.LastName = null;
                if (model.OtherNamesUpdate != null)
                    original.OtherNames = model.OtherNamesUpdate;
                original.DisplayName = original.OtherNames;
            }
            if (model.Address != null) original.Address = model.Address;
            if (model.Email != null) original.Email = model.Email;
            if (model.MobileNumber != null) original.MobileNumber = model.MobileNumber;
            if (model.PhoneNumber != null) original.PhoneNumber = model.PhoneNumber;
            var result = await _userManager.UpdateAsync(original);
            if (!result.Succeeded)
                return false;
            if (model.Roles != null && model.Roles.Count > 0)
            {
                var existingRoles = await _userManager.GetRolesAsync(original);
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(original, existingRoles);
                if (!removeRolesResult.Succeeded)
                {
                    _logger.LogError($"Failed to remove existing roles for user {original.UserName}");
                    return false;
                }

                var addRolesResult = await _userManager.AddToRolesAsync(original, model.Roles);
                if (!addRolesResult.Succeeded)
                {
                    _logger.LogError($"Failed to add new roles for user {original.UserName}");
                    return false;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error editing user{ex}");
            throw new BadHttpRequestException($"Error editing user {ex.Message}");
        }
    }
}
