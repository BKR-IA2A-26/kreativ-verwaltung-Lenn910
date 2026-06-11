using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Brawlstars_Stats.Models;

namespace Brawlstars_Stats.Controllers
{
    public class BrawlerController : Controller
    {
        private readonly BrawlDbContext _context;

        public BrawlerController(BrawlDbContext context)
        {
            _context = context;
        }

        // READ: Alle Brawler anzeigen
        // URL: /Brawler
        public async Task<IActionResult> Index()
        {
            var brawler = await _context.Brawlers.OrderBy(b => b.Name).ToListAsync();
            return View(brawler);
        }

        // CREATE: Brawler hinzufügen (Formular anzeigen)
        public IActionResult Create()
        {
            return View();
        }

        // CREATE: Brawler speichern
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brawler neuerBrawler)
        {
            if (ModelState.IsValid)
            {
                _context.Brawlers.Add(neuerBrawler);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(neuerBrawler);
        }
    }
}