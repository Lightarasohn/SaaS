using System;
using System.Collections.Generic;

namespace SaaS.Microservices.CMS.Models;

public partial class OrgUnit
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid CompanyId { get; set; }

    public int? ParentId { get; set; }

    public string Path { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();

    public virtual ICollection<OrgUnit> InverseParent { get; set; } = new List<OrgUnit>();

    public virtual OrgUnit? Parent { get; set; }
}
