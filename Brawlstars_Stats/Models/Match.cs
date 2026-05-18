using System;
using System.Collections.Generic;

namespace Brawlstars_Stats.Models;

public partial class Match
{
    public int MatchId { get; set; }

    public string? Kuerzel { get; set; }

    public int? BrawlerId { get; set; }

    public int? ModiId { get; set; }

    public int? MapId { get; set; }

    public virtual Brawler? Brawler { get; set; }

    public virtual SpielerInfo? KuerzelNavigation { get; set; }

    public virtual Map? Map { get; set; }

    public virtual Modi? Modi { get; set; }

    public virtual ICollection<Werte> Wertes { get; set; } = new List<Werte>();
}
