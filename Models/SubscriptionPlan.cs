using System;
using System.Collections.Generic;

namespace SaaS.Models;

public partial class SubscriptionPlan
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<CompanySubscription> CompanySubscriptions { get; set; } = new List<CompanySubscription>();

    public virtual ICollection<Module> Modules { get; set; } = new List<Module>();
}
