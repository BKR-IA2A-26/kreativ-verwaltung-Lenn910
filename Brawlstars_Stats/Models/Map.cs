using System;
using System.Collections.Generic;

namespace Brawlstars_Stats.Models;

public partial class Map
{
    public int MapId { get; set; }

    public string MapBezeichnung { get; set; } = null!;

    public string? EmpfohlenerTyp { get; set; }

    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}
