using System;
using System.Collections.Generic;

namespace WebApplication2.Models;

public partial class LotDetail
{
    public int LotId { get; set; }

    public string? ServerName { get; set; }

    public int? SellerId { get; set; }

    public int? Amount { get; set; }

    public string? DescriptionLot { get; set; }

    public string? Price { get; set; }

    public int? GamesCategoryId { get; set; }

    public virtual GamesCategory? GamesCategory { get; set; }

    public virtual Seller? Seller { get; set; }
}
