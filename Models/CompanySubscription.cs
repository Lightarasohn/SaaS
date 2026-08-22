using System;
using System.Collections.Generic;

namespace CMS.Models;

public partial class CompanySubscription
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int PlanId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsActive { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual SubscriptionPlan Plan { get; set; } = null!;
}
