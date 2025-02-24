using System;
using System.Collections.Generic;

namespace WebApplication2.Models;

public partial class Seller
{
    public int SellerId { get; set; }

    public string? Sellername { get; set; }

    public int? ReviewCount { get; set; }

    public string? SellerInfo { get; set; }

    public string? RatingStar { get; set; }

    public virtual ICollection<LotDetail> LotDetails { get; set; } = new List<LotDetail>();
}
