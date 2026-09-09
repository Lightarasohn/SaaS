using System;
using System.Collections.Generic;

namespace SaaS.Microservices.CMS.Models;

public partial class ExpenseCategory
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
