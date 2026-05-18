using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Brawlstars_Stats.Models; // Wichtig, um deine Models zu laden

namespace Brawlstars_Stats.Controllers
{
    public class MapController : Controller
    {
        private readonly BrawlDbContext _context;

        // Per Dependency Injection holen wir uns den Datenbank-Kontext
        public MapController(BrawlDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. READ (Alle Maps anzeigen)
        // URL: /Map
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var maps = await _context.Maps.OrderBy(m => m.MapId).ToListAsync();
            return View(maps); // Gibt die Liste an die HTML-Ansicht weiter
        }

        // ==========================================
        // 2. CREATE (Formular anzeigen & speichern)
        // URL: /Map/Create
        // ==========================================
        // GET: Zeigt das leere Formular an
        public IActionResult Create()
        {
            return View();
        }

        // POST: Verarbeitet die eingegebenen Daten des Formulars
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Map neueMap)
        {
            if (ModelState.IsValid)
            {
                _context.Maps.Add(neueMap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); // Zurück zur Liste
            }
            return View(neueMap);
        }

        // ==========================================
        // 3. UPDATE (Bestehende Map bearbeiten)
        // URL: /Map/Edit/5
        // ==========================================
        // GET: Lädt die Map und zeigt sie im Formular an
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var map = await _context.Maps.FindAsync(id);
            if (map == null) return NotFound();

            return View(map);
        }

        // POST: Speichert die Änderungen ab
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Map bearbeiteteMap)
        {
            if (id != bearbeiteteMap.MapId) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Maps.Update(bearbeiteteMap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(bearbeiteteMap);
        }

        // ==========================================
        // 4. DELETE (Löschen)
        // URL: /Map/Delete/5
        // ==========================================
        // GET: Zeigt eine Sicherheitsabfrage an
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var map = await _context.Maps.FindAsync(id);
            if (map == null) return NotFound();

            return View(map);
        }

        // POST: Bestätigt das endgültige Löschen aus der DB
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var map = await _context.Maps.FindAsync(id);
            if (map != null)
            {
                _context.Maps.Remove(map);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}