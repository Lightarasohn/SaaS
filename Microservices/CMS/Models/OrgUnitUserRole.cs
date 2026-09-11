using System;
using System.Collections.Generic;

namespace SaaS.Microservices.CMS.Models;

public partial class OrgUnitUserRole
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public int OrgUnitId { get; set; }

    public Guid UserId { get; set; }

    public int RoleId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public virtual OrgUnit OrgUnit { get; set; } = null!;

    public virtual OrgUnitRole Role { get; set; } = null!;
}
