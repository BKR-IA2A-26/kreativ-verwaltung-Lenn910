using System.Diagnostics;
using Brawlstars_Stats.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Brawlstars_Stats.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BrawlDbContext _context;

        public HomeController(ILogger<HomeController> logger, BrawlDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData()
        {
            // Fetch last 10 matches with values
            var recentMatches = await _context.Matches
                .Include(m => m.Wertes)
                .OrderByDescending(m => m.MatchId)
                .Take(10)
                .ToListAsync();

            // Reverse to get chronological order for the line chart
            recentMatches.Reverse();

            var kdTrend = recentMatches.Select(m => {
                var werte = m.Wertes.FirstOrDefault();
                if (werte == null) return 0f;
                int kills = werte.Kills ?? 0;
                int deaths = werte.Tode ?? 0;
                return deaths == 0 ? kills : (float)kills / deaths;
            }).ToList();

            var matchLabels = recentMatches.Select(m => $"Match {m.MatchId}").ToList();

            // Fetch Brawler Type Performance for Radar Chart (Load to memory first due to complex C# logic)
            var allMatches = await _context.Matches
                .Include(m => m.Brawler)
                .Include(m => m.Wertes)
                .Where(m => m.Brawler != null && m.Brawler.Typ != null)
                .ToListAsync();

            var typePerformance = allMatches
                .GroupBy(m => m.Brawler!.Typ)
                .Select(g => new {
                    Type = g.Key,
                    AverageKD = g.Average(m => {
                        var werte = m.Wertes.FirstOrDefault();
                        if (werte == null) return 0f;
                        int kills = werte.Kills ?? 0;
                        int deaths = werte.Tode ?? 0;
                        return deaths == 0 ? kills : (float)kills / deaths;
                    })
                })
                .ToList();

            var types = typePerformance.Select(t => t.Type).ToList();
            var typeAverages = typePerformance.Select(t => t.AverageKD).ToList();

            // Trophy Trend (last 20 matches, cumulative)
            var trophyMatches = await _context.Wertes
                .OrderByDescending(w => w.WerteId)
                .Take(20)
                .Select(w => w.PokalVeraenderung ?? 0)
                .ToListAsync();
            
            trophyMatches.Reverse();
            var trophyCumulative = new List<int>();
            int tSum = 0;
            foreach (var tc in trophyMatches)
            {
                tSum += tc;
                trophyCumulative.Add(tSum);
            }

            return Json(new { 
                kdLabels = matchLabels, 
                kdData = kdTrend,
                radarLabels = types,
                radarData = typeAverages,
                trophyLabels = trophyMatches.Select((_, i) => $"Spiel {i + 1}").ToList(),
                trophyPerMatch = trophyMatches,
                trophyCumulative = trophyCumulative
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
