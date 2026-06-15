using Brawlstars_Stats.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Brawlstars_Stats.Controllers
{
    public class MetaAnalyzerController : Controller
    {
        private readonly BrawlDbContext _context;

        public MetaAnalyzerController(BrawlDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["MapId"] = new SelectList(await _context.Maps.ToListAsync(), "MapId", "MapBezeichnung");
            ViewData["ModiId"] = new SelectList(await _context.Modis.ToListAsync(), "ModiId", "Bezeichnung");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRecommendations(int mapId, int modiId)
        {
            var matches = await _context.Matches
                .Include(m => m.Brawler)
                .Include(m => m.Wertes)
                .Where(m => m.MapId == mapId && m.ModiId == modiId && m.Brawler != null)
                .ToListAsync();

            if (!matches.Any())
            {
                return Json(new { success = false, message = "Nicht genügend Daten für diese Kombination." });
            }

            var recommendations = matches
                .GroupBy(m => m.Brawler!.Name)
                .Select(g => new
                {
                    BrawlerName = g.Key,
                    GamesPlayed = g.Count(),
                    AverageKills = g.Average(m => m.Wertes.FirstOrDefault()?.Kills ?? 0),
                    AverageDeaths = g.Average(m => m.Wertes.FirstOrDefault()?.Tode ?? 0),
                    WinRate = g.Average(m => {
                        var w = m.Wertes.FirstOrDefault();
                        // Assume Platz <= 4 in Showdown or Platz == 1 in 3v3 is a win.
                        // Simple proxy: if Kills > Deaths it's a "good game" or if Platz is good.
                        if (w == null) return 0.0;
                        bool isWin = (w.Platz != null && w.Platz <= 4) || ((w.Kills ?? 0) >= (w.Tode ?? 0));
                        return isWin ? 100.0 : 0.0;
                    })
                })
                .OrderByDescending(r => r.WinRate)
                .ThenByDescending(r => r.AverageKills)
                .ToList();

            return Json(new { success = true, data = recommendations });
        }
    }
}
