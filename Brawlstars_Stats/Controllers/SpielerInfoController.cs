using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Brawlstars_Stats.Models;

namespace Brawlstars_Stats.Controllers
{
    public class SpielerInfoController : Controller
    {
        private readonly BrawlDbContext _context;

        public SpielerInfoController(BrawlDbContext context)
        {
            _context = context;
        }

        // READ: Alle Spieler anzeigen
        // URL: /SpielerInfo
        // READ: Alle Spieler anzeigen
        public async Task<IActionResult> Index()
        {
            var spieler = await _context.SpielerInfos.OrderBy(s => s.Kuerzel).ToListAsync();
            return View(spieler);
        }

        // CREATE: Spieler hinzufügen (Formular anzeigen)
        public IActionResult Create()
        {
            return View();
        }

        // CREATE: Spieler speichern
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SpielerInfo neuerSpieler)
        {
            if (ModelState.IsValid)
            {
                _context.SpielerInfos.Add(neuerSpieler);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(neuerSpieler);
        }
    }
}