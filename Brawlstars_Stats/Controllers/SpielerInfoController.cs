using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Brawlstars_Stats.Models;
using Brawlstars_Stats.Services;

namespace Brawlstars_Stats.Controllers
{
    public class SpielerInfoController : Controller
    {
        private readonly BrawlDbContext _context;
        private readonly BrawlStarsApiService _apiService;

        public SpielerInfoController(BrawlDbContext context, BrawlStarsApiService apiService)
        {
            _context = context;
            _apiService = apiService;
        }

        // READ: Alle Spieler anzeigen + Live-Profil des ersten Spielers
        public async Task<IActionResult> Index()
        {
            var spieler = await _context.SpielerInfos.OrderBy(s => s.Kuerzel).ToListAsync();

            // Try to load live profile for the first player
            PlayerProfileResponse? liveProfile = null;
            if (spieler.Any())
            {
                try
                {
                    liveProfile = await _apiService.GetLivePlayerProfileAsync(spieler.First().Kuerzel);
                }
                catch
                {
                    // API call failed – show DB data only
                }
            }

            // Calculate trophy trend from local DB
            var trophyTrend = await _context.Wertes
                .OrderByDescending(w => w.WerteId)
                .Take(30)
                .Select(w => new { w.WerteId, w.PokalVeraenderung })
                .ToListAsync();

            trophyTrend.Reverse(); // Chronological order

            ViewBag.LiveProfile = liveProfile;
            ViewBag.TrophyTrendLabels = trophyTrend.Select((t, i) => $"Spiel {i + 1}").ToList();
            ViewBag.TrophyTrendData = trophyTrend.Select(t => t.PokalVeraenderung ?? 0).ToList();

            // Calculate cumulative trophy change
            var cumulative = new List<int>();
            int sum = 0;
            foreach (var t in trophyTrend)
            {
                sum += t.PokalVeraenderung ?? 0;
                cumulative.Add(sum);
            }
            ViewBag.TrophyCumulative = cumulative;

            // Push Recommendations: Brawlers with high power but low trophies
            ViewBag.PushRecommendations = liveProfile?.Brawlers?
                .Where(b => b.Power >= 7)
                .OrderBy(b => b.Trophies)
                .Take(5)
                .ToList() ?? new List<PlayerBrawlerStat>();

            return View(spieler);
        }

        // AJAX: Get Live Profile Data
        [HttpGet]
        public async Task<IActionResult> GetLiveProfile(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return Json(new { success = false });

            try
            {
                var profile = await _apiService.GetLivePlayerProfileAsync(tag);
                if (profile == null) return Json(new { success = false });

                return Json(new
                {
                    success = true,
                    name = profile.Name,
                    tag = profile.Tag,
                    trophies = profile.Trophies,
                    highestTrophies = profile.HighestTrophies,
                    expLevel = profile.ExpLevel,
                    victories3v3 = profile.Victories3v3,
                    soloVictories = profile.SoloVictories,
                    duoVictories = profile.DuoVictories,
                    club = profile.Club?.Name ?? "Kein Club",
                    brawlerCount = profile.Brawlers?.Count ?? 0,
                    brawlers = profile.Brawlers?.OrderByDescending(b => b.Trophies).Select(b => new
                    {
                        name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(b.Name.ToLower()),
                        power = b.Power,
                        rank = b.Rank,
                        trophies = b.Trophies,
                        highestTrophies = b.HighestTrophies
                    }).ToList()
                });
            }
            catch
            {
                return Json(new { success = false });
            }
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