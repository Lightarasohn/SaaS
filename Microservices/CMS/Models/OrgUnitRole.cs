using System;
using System.Collections.Generic;

namespace SaaS.Microservices.CMS.Models;

public partial class OrgUnitRole
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<OrgUnitUserRole> OrgUnitUserRoles { get; set; } = new List<OrgUnitUserRole>();
}
