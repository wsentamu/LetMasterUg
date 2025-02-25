using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LetMasterWebApp.Core;
using LetMasterWebApp.DataLayer;

namespace LetMasterWebApp.Pages.ExpenseTypeMgt
{
    public class DetailsModel : PageModel
    {
        private readonly LetMasterWebApp.DataLayer.ApplicationDbContext _context;

        public DetailsModel(LetMasterWebApp.DataLayer.ApplicationDbContext context)
        {
            _context = context;
        }

        public ExpenseType ExpenseType { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var expensetype = await _context.ExpenseTypes.FirstOrDefaultAsync(m => m.Id == id);
            if (expensetype == null)
            {
                return NotFound();
            }
            else
            {
                ExpenseType = expensetype;
            }
            return Page();
        }
    }
}
