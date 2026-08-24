using System;
using System.Collections.Generic;

namespace SaaS.Microservices.CMS.Models;

public partial class Distributor
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public Guid PublicId { get; set; }

    public string Region { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}
