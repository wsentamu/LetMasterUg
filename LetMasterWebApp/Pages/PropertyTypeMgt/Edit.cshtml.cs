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

namespace LetMasterWebApp.Pages.Admin
{
    public class EditModel : PageModel
    {
        private readonly LetMasterWebApp.DataLayer.ApplicationDbContext _context;

        public EditModel(LetMasterWebApp.DataLayer.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public PropertyType PropertyType { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var propertytype =  await _context.PropertyTypes.FirstOrDefaultAsync(m => m.Id == id);
            if (propertytype == null)
            {
                return NotFound();
            }
            PropertyType = propertytype;
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
            PropertyType.UpdatedDate=DateTime.UtcNow;
            _context.Attach(PropertyType).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PropertyTypeExists(PropertyType.Id))
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

        private bool PropertyTypeExists(int id)
        {
            return _context.PropertyTypes.Any(e => e.Id == id);
        }
    }
}
