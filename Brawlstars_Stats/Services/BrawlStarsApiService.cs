using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Brawlstars_Stats.Models;
using Microsoft.EntityFrameworkCore;

namespace Brawlstars_Stats.Services
{
    public class BrawlStarsApiService
    {
        private readonly HttpClient _httpClient;
        private readonly BrawlDbContext _context;
        private readonly string _token;

        public BrawlStarsApiService(HttpClient httpClient, BrawlDbContext context, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _context = context;
            _token = configuration["BrawlStarsApi:Token"] ?? "";
            
            // Configure HTTP Client
            _httpClient.BaseAddress = new Uri("https://api.brawlstars.com/v1/");
        }

        public async Task<bool> SyncBattleLogAsync(string playerTag)
        {
            if (string.IsNullOrEmpty(_token))
            {
                throw new InvalidOperationException("API Token fehlt in appsettings.json!");
            }

            // Standardize player tag: must start with '#' internally but URL needs '%23'
            string cleanTag = playerTag.Trim();
            if (!cleanTag.StartsWith("#"))
            {
                cleanTag = "#" + cleanTag;
            }

            string urlEncodedTag = HttpUtility.UrlEncode(cleanTag);
            
            var request = new HttpRequestMessage(HttpMethod.Get, $"players/{urlEncodedTag}/battlelog");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var battleLog = JsonSerializer.Deserialize<BattleLogResponse>(jsonString, options);
            if (battleLog?.Items == null)
            {
                return false;
            }

            // Ensure Player exists in SpielerInfo
            var spieler = await _context.SpielerInfos.FirstOrDefaultAsync(s => s.Kuerzel == cleanTag);
            if (spieler == null)
            {
                spieler = new SpielerInfo
                {
                    Kuerzel = cleanTag,
                    Attribute = "Brawler (" + cleanTag + ")", // default name
                    GesamteKills = 0,
                    GesamteTode = 0
                };
                _context.SpielerInfos.Add(spieler);
                await _context.SaveChangesAsync();
            }

            int newMatchesCount = 0;

            foreach (var item in battleLog.Items)
            {
                // Find or create Brawler
                var apiBrawler = GetPlayerBrawler(item, cleanTag);
                if (apiBrawler == null) continue;

                string brawlerName = FormatName(apiBrawler.Name);
                var brawler = await _context.Brawlers.FirstOrDefaultAsync(b => b.Name.ToLower() == brawlerName.ToLower());
                if (brawler == null)
                {
                    // Create new brawler
                    // Supercell brawler IDs are e.g. 16000000. Let's use it or fallback to a new unique ID if ID already exists.
                    int brawlerId = apiBrawler.Id;
                    while (await _context.Brawlers.AnyAsync(b => b.BrawlerId == brawlerId))
                    {
                        brawlerId++;
                    }

                    brawler = new Brawler
                    {
                        BrawlerId = brawlerId,
                        Name = brawlerName,
                        Hp = 4000, // placeholder default
                        Ang = 1000, // placeholder default
                        Typ = "Unbekannt",
                        Seltenheit = "Selten",
                        Skin = "Standard"
                    };
                    _context.Brawlers.Add(brawler);
                    await _context.SaveChangesAsync();
                }

                // Find or create Map
                string mapName = item.Event?.Map ?? "Unbekannte Map";
                var map = await _context.Maps.FirstOrDefaultAsync(m => m.MapBezeichnung.ToLower() == mapName.ToLower());
                if (map == null)
                {
                    int mapId = item.Event?.Id ?? new Random().Next(10000000, 99999999);
                    while (await _context.Maps.AnyAsync(m => m.MapId == mapId))
                    {
                        mapId++;
                    }

                    map = new Map
                    {
                        MapId = mapId,
                        MapBezeichnung = mapName,
                        EmpfohlenerTyp = brawler.Typ
                    };
                    _context.Maps.Add(map);
                    await _context.SaveChangesAsync();
                }

                // Find or create Modi
                string modeName = FormatName(item.Battle?.Mode ?? item.Event?.Mode ?? "Unbekannt");
                var modi = await _context.Modis.FirstOrDefaultAsync(m => m.Bezeichnung.ToLower() == modeName.ToLower());
                if (modi == null)
                {
                    int modiId = new Random().Next(1000, 9999);
                    while (await _context.Modis.AnyAsync(m => m.ModiId == modiId))
                    {
                        modiId++;
                    }

                    modi = new Modi
                    {
                        ModiId = modiId,
                        Bezeichnung = modeName,
                        SpielerInsg = item.Battle?.Players?.Count ?? 6,
                        TeamGroesse = item.Battle?.Teams?.FirstOrDefault()?.Count ?? 3,
                        Gift = modeName.ToLower().Contains("showdown")
                    };
                    _context.Modis.Add(modi);
                    await _context.SaveChangesAsync();
                }

                // Proxy stats for DB compatibility:
                // Since 3v3 API doesn't give kills/deaths:
                // Victory: Kills = 1, Tode = 0
                // Defeat: Kills = 0, Tode = 1
                // Showdown: Use rank/placement. Rank <= 4 is a victory.
                int kills = 0;
                int deaths = 0;
                int? placement = null;
                bool isStarplayer = false;

                if (modi.Gift) // Showdown
                {
                    placement = GetShowdownRank(item, cleanTag);
                    if (placement <= 4)
                    {
                        kills = 1;
                        deaths = 0;
                    }
                    else
                    {
                        kills = 0;
                        deaths = 1;
                    }
                }
                else // 3v3
                {
                    string result = item.Battle?.Result ?? "defeat";
                    if (result.ToLower() == "victory")
                    {
                        kills = 1;
                        deaths = 0;
                    }
                    else
                    {
                        kills = 0;
                        deaths = 1;
                    }

                    // Check if Starplayer
                    if (item.Battle?.StarPlayer?.Tag == cleanTag)
                    {
                        isStarplayer = true;
                    }
                }

                // Double Import Protection:
                // Check if exact match signature exists (Kuerzel + BrawlerId + ModiId + MapId)
                // AND there exists a Werte entry with same values.
                var existingMatch = await _context.Matches
                    .Include(m => m.Wertes)
                    .Where(m => m.Kuerzel == cleanTag && 
                                m.BrawlerId == brawler.BrawlerId && 
                                m.ModiId == modi.ModiId && 
                                m.MapId == map.MapId)
                    .FirstOrDefaultAsync(m => m.Wertes.Any(w => w.Kills == kills && 
                                                               w.Tode == deaths && 
                                                               w.Platz == placement && 
                                                               w.Starspieler == isStarplayer));

                if (existingMatch != null)
                {
                    // Match already imported, skip
                    continue;
                }

                // Save Match
                var match = new Match
                {
                    Kuerzel = cleanTag,
                    BrawlerId = brawler.BrawlerId,
                    ModiId = modi.ModiId,
                    MapId = map.MapId
                };

                _context.Matches.Add(match);
                await _context.SaveChangesAsync();

                // Save Werte
                var werte = new Werte
                {
                    MatchId = match.MatchId,
                    Kills = kills,
                    Tode = deaths,
                    Schaden = kills * 15000, // mock damage based on win/loss
                    Platz = placement,
                    Starspieler = isStarplayer,
                    PokalVeraenderung = item.Battle?.TrophyChange ?? 0
                };

                _context.Wertes.Add(werte);
                await _context.SaveChangesAsync();

                // Update SpielerInfo totals
                spieler.GesamteKills = (spieler.GesamteKills ?? 0) + kills;
                spieler.GesamteTode = (spieler.GesamteTode ?? 0) + deaths;
                
                newMatchesCount++;
            }

            if (newMatchesCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return true;
        }

        private BrawlerInfo? GetPlayerBrawler(BattleLogItem item, string playerTag)
        {
            if (item.Battle == null) return null;

            // 1. Try from Teams (3v3)
            if (item.Battle.Teams != null)
            {
                foreach (var team in item.Battle.Teams)
                {
                    var player = team.FirstOrDefault(p => p.Tag == playerTag);
                    if (player?.Brawler != null) return player.Brawler;
                }
            }

            // 2. Try from Players (Showdown)
            if (item.Battle.Players != null)
            {
                var player = item.Battle.Players.FirstOrDefault(p => p.Tag == playerTag);
                if (player?.Brawler != null) return player.Brawler;
            }

            return null;
        }

        private int? GetShowdownRank(BattleLogItem item, string playerTag)
        {
            if (item.Battle?.Rank != null) return item.Battle.Rank;
            
            // Fallback: check if we can infer it
            return null;
        }

        private string FormatName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return "Unbekannt";
            
            // Format camelCase or UPPERCASE to Title Case (e.g. brawlBall -> Brawlball, SHELLY -> Shelly)
            // Replace camelCase capitals with space or just convert to title case
            string clean = rawName.Replace("brawlBall", "Brawlball")
                                  .Replace("gemGrab", "Juwelenjagd")
                                  .Replace("soloShowdown", "Solo Showdown")
                                  .Replace("duoShowdown", "Duo Showdown")
                                  .Replace("heist", "Tresorraub")
                                  .Replace("bounty", "Kopfgeldjagd")
                                  .Replace("hotZone", "Heiße Zone")
                                  .Replace("knockout", "Knockout");

            if (clean == rawName)
            {
                // Fallback basic formatting
                clean = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(rawName.ToLower());
            }

            return clean;
        }

        // ===============================================
        // 2. LIVE PLAYER PROFILE
        // ===============================================
        public async Task<PlayerProfileResponse?> GetLivePlayerProfileAsync(string playerTag)
        {
            if (string.IsNullOrEmpty(_token)) return null;

            string cleanTag = playerTag.Trim();
            if (!cleanTag.StartsWith("#")) cleanTag = "#" + cleanTag;
            string urlEncodedTag = HttpUtility.UrlEncode(cleanTag);
            
            var request = new HttpRequestMessage(HttpMethod.Get, $"players/{urlEncodedTag}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            
            return JsonSerializer.Deserialize<PlayerProfileResponse>(jsonString, options);
        }
    }

    // JSON Deserialization Classes for Profile
    public class PlayerProfileResponse
    {
        public string Tag { get; set; } = "";
        public string Name { get; set; } = "";
        public int Trophies { get; set; }
        public int HighestTrophies { get; set; }
        public int ExpLevel { get; set; }
        [JsonPropertyName("3vs3Victories")]
        public int Victories3v3 { get; set; }
        public int SoloVictories { get; set; }
        public int DuoVictories { get; set; }
        public PlayerClub? Club { get; set; }
        public List<PlayerBrawlerStat> Brawlers { get; set; } = new();
    }

    public class PlayerClub
    {
        public string Name { get; set; } = "";
    }

    public class PlayerBrawlerStat
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Power { get; set; }
        public int Rank { get; set; }
        public int Trophies { get; set; }
        public int HighestTrophies { get; set; }
    }

    // JSON Deserialization Classes for Battlelog
    public class BattleLogResponse
    {
        public List<BattleLogItem> Items { get; set; } = new();
    }

    public class BattleLogItem
    {
        public string BattleTime { get; set; } = "";
        public EventInfo? Event { get; set; }
        public BattleDetails? Battle { get; set; }
    }

    public class EventInfo
    {
        public int Id { get; set; }
        public string Mode { get; set; } = "";
        public string Map { get; set; } = "";
    }

    public class BattleDetails
    {
        public string Mode { get; set; } = "";
        public string Type { get; set; } = "";
        public string Result { get; set; } = "";
        public int? Duration { get; set; }
        public int? TrophyChange { get; set; }
        public int? Rank { get; set; }
        public StarPlayerInfo? StarPlayer { get; set; }
        public List<List<PlayerInfo>>? Teams { get; set; }
        public List<PlayerInfo>? Players { get; set; }
    }

    public class StarPlayerInfo
    {
        public string Tag { get; set; } = "";
        public string Name { get; set; } = "";
        public BrawlerInfo? Brawler { get; set; }
    }

    public class PlayerInfo
    {
        public string Tag { get; set; } = "";
        public string Name { get; set; } = "";
        public BrawlerInfo? Brawler { get; set; }
    }

    public class BrawlerInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Power { get; set; }
        public int Trophies { get; set; }
    }
}
