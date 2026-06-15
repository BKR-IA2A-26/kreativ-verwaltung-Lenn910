using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Brawlstars_Stats.Models;

namespace Brawlstars_Stats.Controllers
{
    public class ModiController : Controller
    {
        private readonly BrawlDbContext _context;

        public ModiController(BrawlDbContext context)
        {
            _context = context;
        }

        // READ: Alle Modi anzeigen
        // URL: /Modi
        public async Task<IActionResult> Index()
        {
            // Falls EF Core deine Tabelle "Modis" genannt hat, lass es so. 
            // Falls es "Modis" nicht findet, versuche "_context.Modis" oder "_context.ModisTables"
            var modi = await _context.Modis.OrderBy(m => m.ModiId).ToListAsync();
            return View(modi);
        }

        // CREATE: Modus hinzufügen (Formular anzeigen)
        public IActionResult Create()
        {
            return View();
        }

        // CREATE: Modus speichern
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Modi neuerModus)
        {
            if (ModelState.IsValid)
            {
                _context.Modis.Add(neuerModus);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(neuerModus);
        }
    }
}