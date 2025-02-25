using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LetMasterWebApp.Core;
using LetMasterWebApp.DataLayer;

namespace LetMasterWebApp.Pages.UnitTypeMgt
{
    public class EditModel : PageModel
    {
        private readonly LetMasterWebApp.DataLayer.ApplicationDbContext _context;

        public EditModel(LetMasterWebApp.DataLayer.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public UnitType UnitType { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var unittype =  await _context.UnitTypes.FirstOrDefaultAsync(m => m.Id == id);
            if (unittype == null)
            {
                return NotFound();
            }
            UnitType = unittype;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            UnitType.UpdatedDate=DateTime.UtcNow;
            _context.Attach(UnitType).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UnitTypeExists(UnitType.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool UnitTypeExists(int id)
        {
            return _context.UnitTypes.Any(e => e.Id == id);
        }
    }
}
