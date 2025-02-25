using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LetMasterWebApp.Core;
using LetMasterWebApp.DataLayer;

namespace LetMasterWebApp.Pages.UnitTypeMgt
{
    public class DetailsModel : PageModel
    {
        private readonly LetMasterWebApp.DataLayer.ApplicationDbContext _context;

        public DetailsModel(LetMasterWebApp.DataLayer.ApplicationDbContext context)
        {
            _context = context;
        }

        public UnitType UnitType { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var unittype = await _context.UnitTypes.FirstOrDefaultAsync(m => m.Id == id);
            if (unittype == null)
            {
                return NotFound();
            }
            else
            {
                UnitType = unittype;
            }
            return Page();
        }
    }
}
