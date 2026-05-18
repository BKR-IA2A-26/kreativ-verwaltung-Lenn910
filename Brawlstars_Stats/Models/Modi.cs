using System;
using System.Collections.Generic;

namespace Brawlstars_Stats.Models;

public partial class Modi
{
    public int ModiId { get; set; }

    public string Bezeichnung { get; set; } = null!;

    public int SpielerInsg { get; set; }

    public int TeamGroesse { get; set; }

    public bool Gift { get; set; }

    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}
