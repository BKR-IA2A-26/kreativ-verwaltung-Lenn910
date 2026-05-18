using System;
using System.Collections.Generic;

namespace Brawlstars_Stats.Models;

public partial class Brawler
{
    public int BrawlerId { get; set; }

    public string Name { get; set; } = null!;

    public int Hp { get; set; }

    public int Ang { get; set; }

    public int? Vert { get; set; }

    public int? Lvl { get; set; }

    public string? Typ { get; set; }

    public string? Seltenheit { get; set; }

    public string? Skin { get; set; }

    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}
