using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class Coupon
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string DiscountType { get; set; } = null!;

    public decimal DiscountValue { get; set; }

    public int? MaxUsage { get; set; }

    public int UsedCount { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
