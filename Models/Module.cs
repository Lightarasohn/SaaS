using System;
using System.Collections.Generic;

namespace SaaS.Models;

public partial class Module
{
    public int Id { get; set; }

    public string ModuleKey { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<SubscriptionPlan> Plans { get; set; } = new List<SubscriptionPlan>();
}
