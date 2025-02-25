using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LetMasterWebApp.Core;
using LetMasterWebApp.DataLayer;
using Microsoft.AspNetCore.Authorization;

namespace LetMasterWebApp.Pages.Admin;
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly LetMasterWebApp.DataLayer.ApplicationDbContext _context;

    public IndexModel(LetMasterWebApp.DataLayer.ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<PropertyType> PropertyType { get;set; } = default!;

    public async Task OnGetAsync()
    {
        PropertyType = await _context.PropertyTypes.ToListAsync();
    }
}
