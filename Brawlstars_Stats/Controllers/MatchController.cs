using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Brawlstars_Stats.Models;

namespace Brawlstars_Stats.Controllers
{
    public class MatchController : Controller
    {
        private readonly BrawlDbContext _context;

        public MatchController(BrawlDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. READ: Alle Matches inklusive Werten & Stammdaten anzeigen
        // ==========================================
        public async Task<IActionResult> Index()
        {
            // .Include sorgt dafür, dass SQL-Joins gemacht werden und wir auf Namen zugreifen können!
            var matches = await _context.Matches
                .Include(m => m.Brawler)
                .Include(m => m.Map)
                .Include(m => m.Modi)
                .Include(m => m.Wertes) // Hier laden wir die Werte-Tabelle mit ("Wertes" mit "s")
                .OrderByDescending(m => m.MatchId) // Neueste Matches zuerst
                .ToListAsync();

            return View(matches);
        }

        // ==========================================
        // 2. CREATE: Formular mit Dropdowns anzeigen
        // ==========================================
        public async Task<IActionResult> Create()
        {
            // Wir laden die Listen sortiert aus der Datenbank
            var brawlerList = await _context.Brawlers.OrderBy(b => b.Name).ToListAsync();
            var mapList = await _context.Maps.OrderBy(m => m.MapBezeichnung).ToListAsync();
            var modiList = await _context.Modis.OrderBy(m => m.Bezeichnung).ToListAsync();
            var spielerList = await _context.SpielerInfos.OrderBy(s => s.Kuerzel).ToListAsync();

            // Wir packen die Listen in das ViewData für die HTML-Dropdowns
            ViewData["BrawlerId"] = new SelectList(brawlerList, "BrawlerId", "Name");
            ViewData["MapId"] = new SelectList(mapList, "MapId", "MapBezeichnung");
            ViewData["ModiId"] = new SelectList(modiList, "ModiId", "Bezeichnung");
            ViewData["Kuerzel"] = new SelectList(await _context.SpielerInfos.OrderBy(s => s.Kuerzel).ToListAsync(), "Kuerzel", "Attribute");

            return View();
        }

        // ==========================================
        // 3. CREATE POST: Match und Werte gleichzeitig speichern
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Match neuesMatch)
        {
            if (ModelState.IsValid)
            {
                // EF Core speichert hier automatisch das Match UND die Stats in der Werte-Tabelle!
                _context.Matches.Add(neuesMatch);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Falls beim Speichern etwas schiefging (Validierungsfehler), Dropdowns neu laden
            var brawlerList = await _context.Brawlers.OrderBy(b => b.Name).ToListAsync();
            var mapList = await _context.Maps.OrderBy(m => m.MapBezeichnung).ToListAsync();
            var modiList = await _context.Modis.OrderBy(m => m.Bezeichnung).ToListAsync();
            var spielerList = await _context.SpielerInfos.OrderBy(s => s.Kuerzel).ToListAsync();

            ViewData["BrawlerId"] = new SelectList(brawlerList, "BrawlerId", "Name", neuesMatch.BrawlerId);
            ViewData["MapId"] = new SelectList(mapList, "MapId", "MapBezeichnung", neuesMatch.MapId);
            ViewData["ModiId"] = new SelectList(modiList, "ModiId", "Bezeichnung", neuesMatch.ModiId);
            ViewData["Kuerzel"] = new SelectList(spielerList, "Kuerzel", "Attribute", neuesMatch.Kuerzel);

            return View(neuesMatch);
        }
    }
}