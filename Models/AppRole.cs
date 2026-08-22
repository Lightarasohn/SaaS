using System;
using System.Collections.Generic;

namespace CMS.Models;

public partial class AppRole
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<AppUser> AppUsers { get; set; } = new List<AppUser>();
}
