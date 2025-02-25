using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LetMasterWebApp.Core;
using LetMasterWebApp.DataLayer;

namespace LetMasterWebApp.Pages.Admin
{
    public class DetailsModel : PageModel
    {
        private readonly LetMasterWebApp.DataLayer.ApplicationDbContext _context;

        public DetailsModel(LetMasterWebApp.DataLayer.ApplicationDbContext context)
        {
            _context = context;
        }

        public PropertyType PropertyType { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var propertytype = await _context.PropertyTypes.FirstOrDefaultAsync(m => m.Id == id);
            if (propertytype == null)
            {
                return NotFound();
            }
            else
            {
                PropertyType = propertytype;
            }
            return Page();
        }
    }
}
