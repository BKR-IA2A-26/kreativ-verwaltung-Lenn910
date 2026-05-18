using System;
using System.Collections.Generic;

namespace Brawlstars_Stats.Models;

public partial class SpielerInfo
{
    public string Kuerzel { get; set; } = null!;

    public float? KD { get; set; }

    public float? WinPercentInsg { get; set; }

    public int? GesamteTode { get; set; }

    public int? GesamteKills { get; set; }

    public string? Attribute { get; set; }

    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}
