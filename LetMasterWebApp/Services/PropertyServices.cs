using AutoMapper;
using LetMasterWebApp.Core;
using LetMasterWebApp.DataLayer;
using LetMasterWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LetMasterWebApp.Services;
public interface IPropertyServices
{
    public Task<PaginatedList<PropertyViewModel>> ListPropertiesAsync(PropertySearchModel searchModel);
    public Task<PropertyViewModel> GetPropertyDetailsAsync(int propertyId);
    public Task<List<SelectListItem>> GetPropertyTypes();
    public Task<List<SelectListItem>> GetPropertyManagers(string userId);
    public Task<bool> CreatePropertyAsync(PropertyCreateModel model);
    public Task<bool> UpdatePropertyAsync(PropertyUpdateModel model);
    public Task<List<SelectListItem>> GetUnitTypes();
    public Task<PaginatedList<UnitViewModel>> ListUnitsAsync(UnitSearchModel searchModel);
    public Task<bool> CreateUnitAsync(UnitCreateModel model);
    public Task<UnitViewModel> GetUnitDetailsAsync(int id);
    public Task<bool> UpdateUnitAsync(UnitUpdateModel model);
    public Task<bool> AddExpenseAsync(ExpenseCreateModel model);
    public Task<List<SelectListItem>> GetUnitsByProperty(int propertyId);
    public Task<List<SelectListItem>> GetExpensetypes();
}
public class PropertyServices : IPropertyServices
{
    private ILogger<PropertyServices> _logger { get; set; }
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    public PropertyServices(ILogger<PropertyServices> logger, ApplicationDbContext context, UserManager<User> userManager, IMapper mapper)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
    }
    //list properties
    public async Task<PaginatedList<PropertyViewModel>> ListPropertiesAsync(PropertySearchModel searchModel)
    {
        _logger.LogInformation($"ListProperties: {JsonSerializer.Serialize(searchModel)}");
        try
        {
            if (searchModel == null) throw new BadHttpRequestException("Search Parameters Required");
            IQueryable<Property> query = _context.Properties;
            query = query.Where(q => q.IsActive == searchModel.IsActive);
            if (searchModel.PropertyTypeId != null)
                query = query.Where(q => q.PropertyTypeId.Equals(searchModel.PropertyTypeId));
            if (searchModel.Name != null)
                query = query.Where(q => q.Name!.Contains(searchModel.Name));
            if (!string.IsNullOrEmpty(searchModel.PropertyManagerId))
            {
                var user = await _userManager.FindByIdAsync(searchModel.PropertyManagerId);
                var roles = await _userManager.GetRolesAsync(user!);
                if (!roles.Contains("Admin"))
                    query = query.Where(q => q.PropertyManagerId == searchModel.PropertyManagerId);
            }
            var paginatedData = await PaginationHelper.PaginateAsync(query, searchModel.Page, searchModel.PageSize);

            var finalList = new List<PropertyViewModel>();
            //get the property manager, property type
            foreach (var property in paginatedData.Items)
            {
                var propertyView = _mapper.Map<PropertyViewModel>(property);
                if (!string.IsNullOrEmpty(property.PropertyManagerId))
                {
                    var prtMgr = await _userManager.FindByIdAsync(property.PropertyManagerId);
                    if (prtMgr != null)
                    {
                        if (!string.IsNullOrEmpty(prtMgr.OtherNames))
                            propertyView.PropertyManager = prtMgr.OtherNames;
                        else
                            propertyView.PropertyManager = $"{prtMgr.FirstName} {prtMgr.LastName}";
                    }
                }
                propertyView.PropertyType = await _context.PropertyTypes.Where(t => t.Id == property.PropertyTypeId).Select(t => t.Name).FirstOrDefaultAsync();
                //get total active units and occupied units
                propertyView.OccupiedUnits = await _context.Units.Where(u => u.IsOccupied == true && u.IsActive == true && u.PropertyId == property.Id).CountAsync();
                propertyView.TotalUnits = await _context.Units.Where(u => u.IsActive == true && u.PropertyId == property.Id).CountAsync();
                finalList.Add(propertyView);
            }
            _logger.LogInformation($"ListProperties: {paginatedData.TotalCount} Results");
            var paginatedList = new PaginatedList<PropertyViewModel>(finalList, paginatedData.TotalCount, paginatedData.CurrentPage, paginatedData.PageSize);
            return paginatedList;
        }
        catch (Exception ex)
        {
            _logger.LogError($"ListProperties: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    //create property
    public async Task<bool> CreatePropertyAsync(PropertyCreateModel model)
    {
        _logger.LogInformation($"SavePropertyAsync: {JsonSerializer.Serialize(model)}");
        try
        {
            var property = _mapper.Map<Property>(model);
            if (property == null) throw new BadHttpRequestException($"No data submitted");
            await _context.Properties.AddAsync(property);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"SavePropertyAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    //update property
    public async Task<bool> UpdatePropertyAsync(PropertyUpdateModel model)
    {
        _logger.LogInformation($"UpdatePropertyAsync: request {JsonSerializer.Serialize(model)}");
        try
        {
            if (model == null) throw new BadHttpRequestException("No data submitted");
            var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == model.Id);
            bool updated = false;
            if (property == null)
                throw new BadHttpRequestException($"Property with Id: {model.Id} not found");
            if (!string.IsNullOrEmpty(property.Name) && property.Name != model.Name)
            {
                updated = true;
                property.Name = model.Name;
            }
            if (!string.IsNullOrEmpty(model.Address) && model.Address != property.Address)
            {
                updated = true;
                property.Address = model.Address;
            }
            if (!string.IsNullOrEmpty(model.Description) && property.Description != model.Description)
            {
                updated = true;
                property.Description = model.Description;
            }
            if (!string.IsNullOrEmpty(model.Longitude) && property.Longitude != model.Longitude)
            {
                updated = true;
                property.Longitude = model.Longitude;
            }
            if (!string.IsNullOrEmpty(model.Latitude) && property.Latitude != model.Latitude)
            {
                updated = true;
                property.Latitude = model.Latitude;
            }
            if (property.IsActive != model.IsActive)
            {
                updated = true;
                property.IsActive = model.IsActive;
            }
            if (updated)
            {
                property.UpdatedBy = model.UpdatedBy;
                property.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return true;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError($"UpdatePropertyAsync: database update exception {dbEx}");
            throw new BadHttpRequestException("A database error occurred while updating the property.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"UpdatePropertyAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    //get property details
    public async Task<PropertyViewModel> GetPropertyDetailsAsync(int propertyId)
    {
        _logger.LogInformation($"GetPropertyDetails {propertyId}");
        try
        {
            var property = await _context.Properties.Where(p => p.Id == propertyId).FirstOrDefaultAsync();
            if (property == null)
                throw new BadHttpRequestException("Property Not Found");
            var propertyView = _mapper.Map<PropertyViewModel>(property);
            propertyView.OccupiedUnits = await _context.Units.Where(u => u.IsOccupied && u.IsActive == true && u.PropertyId == propertyId).CountAsync();
            propertyView.TotalUnits = await _context.Units.Where(u => u.IsActive == true && u.PropertyId == propertyId).CountAsync();
            propertyView.PropertyType = await _context.PropertyTypes.Where(t => t.Id == property.PropertyTypeId).Select(t => t.Name).FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(property.PropertyManagerId))
            {
                var prtMgr = await _userManager.FindByIdAsync(property.PropertyManagerId);
                if (prtMgr != null)
                {
                    if (!string.IsNullOrEmpty(prtMgr.OtherNames))
                        propertyView.PropertyManager = prtMgr.OtherNames;
                    else
                        propertyView.PropertyManager = $"{prtMgr.FirstName} {prtMgr.LastName}";
                }
            }
            if (!string.IsNullOrEmpty(property.Longitude) && !string.IsNullOrEmpty(property.Latitude))
                propertyView.Coordinates = $"{property.Latitude}, {property.Longitude}";
            return propertyView;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetPropertyDetails: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    //get property types
    public async Task<List<SelectListItem>> GetPropertyTypes()
    {
        try
        {
            var propertyTypes = await _context.PropertyTypes.Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Id.ToString()
            }).ToListAsync();
            return propertyTypes;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetPropertyTypes: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<List<SelectListItem>> GetPropertyManagers(string userId)
    {
        try
        {
            var managersList = new List<SelectListItem>();
            var user = await _userManager.FindByIdAsync(userId);
            var isAdmin = await _userManager.IsInRoleAsync(user!, "Admin");
            if (isAdmin)
            {
                var managers = await _userManager.GetUsersInRoleAsync("Manager");
                foreach (var manager in managers)
                {
                    string text = manager.DisplayName!;
                    managersList.Add(new SelectListItem
                    {
                        Text = text,
                        Value = $"{manager.Id}"
                    });
                }
            }
            else
            {
                managersList.Add(new SelectListItem
                {
                    Text = user!.DisplayName,
                    Value = user!.Id
                });
            }
            return managersList;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetPropertyManagers: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }

    //unit services
    public async Task<PaginatedList<UnitViewModel>> ListUnitsAsync(UnitSearchModel searchModel)
    {
        _logger.LogInformation($"ListProperties: {JsonSerializer.Serialize(searchModel)}");
        try
        {
            if (searchModel == null) throw new BadHttpRequestException("Search parameters required");
            IQueryable<Unit> query = _context.Units.Where(u => u.PropertyId == searchModel.PropertyId);
            query = query.Where(u => u.IsActive == searchModel.IsActive);
            if (searchModel.UnitTypeId != null)
                query = query.Where(u => u.UnitTypeId == searchModel.UnitTypeId);
            if (searchModel.IsOccupied != null)
                query = query.Where(u => u.IsOccupied == searchModel.IsOccupied);
            if (searchModel.Name != null)
                query = query.Where(u => u.Name!.Contains(searchModel.Name));
            var paginatedData = await PaginationHelper.PaginateAsync(query, searchModel.Page, searchModel.PageSize);

            var finalList = new List<UnitViewModel>();
            foreach (var unit in paginatedData.Items)
            {
                var unitView = _mapper.Map<UnitViewModel>(unit);
                if (unit.UnitTypeId != null)
                    unitView.UnitType = await _context.UnitTypes.Where(u => u.Id == unit.UnitTypeId).Select(u => u.Name).FirstOrDefaultAsync();
                finalList.Add(unitView);
            }
            var paginatedList = new PaginatedList<UnitViewModel>(finalList, paginatedData.TotalCount, paginatedData.CurrentPage, paginatedData.PageSize);
            return paginatedList;
        }
        catch (Exception ex)
        {
            _logger.LogError($"ListUnitsAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<List<SelectListItem>> GetUnitTypes()
    {
        try
        {
            var unitTypes = await _context.UnitTypes.Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Id.ToString()
            }).ToListAsync();
            return unitTypes;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetPropertyTypes: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<bool> CreateUnitAsync(UnitCreateModel model)
    {
        _logger.LogInformation($"CreateUnitAsync: {JsonSerializer.Serialize(model)}");
        try
        {
            if (model == null) throw new BadHttpRequestException("Form data not detected");
            if (model.IsBulkEntry)
            {
                if (model.BulkCount == null)
                    throw new BadHttpRequestException($"Number of units to add required");
                var count = (int)model.BulkCount;
                var pfxPos = model.UnitSeriesPrefix;
                var pfx = !string.IsNullOrEmpty(model.UnitSeries) ? model.UnitSeries : string.Empty;
                var unitsToAdd = new List<Unit>();
                for (int i = 1; i <= count; i++)
                {
                    //generate the unit objects and add to range
                    var unit = _mapper.Map<Unit>(model);
                    var unitName = $"{pfx}-{i}";
                    if (!pfxPos)
                        unitName = $"{i}-{pfx}";
                    unit.Name = unitName;
                    unitsToAdd.Add(unit);
                }
                await _context.Units.AddRangeAsync(unitsToAdd);
                await _context.SaveChangesAsync();
            }
            else
            {
                var unit = _mapper.Map<Unit>(model);
                await _context.Units.AddAsync(unit);
                await _context.SaveChangesAsync();
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"CreateUnitAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<UnitViewModel> GetUnitDetailsAsync(int id)
    {
        _logger.LogInformation($"GetUnitDetailsAsync: {id}");
        try
        {
            var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == id);
            if (unit == null) throw new BadHttpRequestException($"Unit with Id {id} not found");
            var unitView = _mapper.Map<UnitViewModel>(unit);
            return unitView;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetUnitDetailsAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<bool> UpdateUnitAsync(UnitUpdateModel model)
    {
        try
        {
            bool updated = false;
            if (model == null) throw new BadHttpRequestException("No data submitted");
            var unit = await _context.Units.Where(u => u.Id == model.Id).FirstOrDefaultAsync();
            if (unit == null) throw new BadHttpRequestException($"Unit with Id {model.Id} not found");
            if (!string.IsNullOrEmpty(model.Name) && unit.Name != model.Name)
            {
                updated = true;
                unit.Name = model.Name;
            }
            if (!string.IsNullOrEmpty(model.Description) & model.Description != unit.Description)
            {
                updated = true;
                unit.Description = model.Description;
            }
            if (model.StandardRate != null && unit.StandardRate != model.StandardRate)
            {
                updated = true;
                unit.StandardRate = model.StandardRate;
            }
            if (model.IsActive != unit.IsActive)
            {
                updated = true;
                unit.IsActive = model.IsActive;
            }
            if (updated)
            {
                unit.UpdatedBy = model.UpdatedBy;
                unit.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return true;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError($"UpdateUnitAsync: database update exception {dbEx}");
            throw new BadHttpRequestException("A database error occurred while updating the property.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"UpdateUnitAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    public async Task<bool> AddExpenseAsync(ExpenseCreateModel model)
    {
        _logger.LogInformation($"AddExpenseAsync: {JsonSerializer.Serialize(model)}");
        try
        {
            if (model == null) throw new BadHttpRequestException("Form data not detected");
            var expense = _mapper.Map<Expense>(model);
            await _context.Expenses.AddAsync(expense);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"AddExpenseAsync: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    //select list to get units in property
    public async Task<List<SelectListItem>> GetUnitsByProperty(int propertyId)
    {
        try
        {
            var units = await _context.Units.Where(u => u.PropertyId == propertyId && u.IsActive == true).Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Id.ToString()
            }).ToListAsync();
            return units;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetUnitsByProperty: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
    //get expense types into select list
    public async Task<List<SelectListItem>> GetExpensetypes()
    {
        try
        {
            var exptypes = await _context.ExpenseTypes.Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Id.ToString()
            }).ToListAsync();
            return exptypes;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetExpensetypes: exception {ex}");
            throw new BadHttpRequestException(ex.Message);
        }
    }
}