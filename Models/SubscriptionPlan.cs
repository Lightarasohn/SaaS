using System;
using System.Collections.Generic;

namespace SaaS.Models;

public partial class SubscriptionPlan
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool HasBudgetAccess { get; set; }

    public bool HasHrAccess { get; set; }

    public virtual ICollection<CompanySubscription> CompanySubscriptions { get; set; } = new List<CompanySubscription>();
}
