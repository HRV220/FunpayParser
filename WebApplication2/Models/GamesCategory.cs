using System;
using System.Collections.Generic;

namespace WebApplication2.Models;

public partial class GamesCategory
{
    public int GamesCategoryId { get; set; }

    public string? GameName { get; set; }

    public string? Category { get; set; }

    public virtual ICollection<LotDetail> LotDetails { get; set; } = new List<LotDetail>();
}
