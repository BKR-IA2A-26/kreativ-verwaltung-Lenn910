using System;
using System.Collections.Generic;

namespace Brawlstars_Stats.Models;

public partial class Werte
{
    public int WerteId { get; set; }

    public int? MatchId { get; set; }

    public int? Platz { get; set; }

    public bool? Starspieler { get; set; }

    public int? Schaden { get; set; }

    public int? Tode { get; set; }

    public int? Kills { get; set; }

    public int? PokalVeraenderung { get; set; }

    public virtual Match? Match { get; set; }
}
