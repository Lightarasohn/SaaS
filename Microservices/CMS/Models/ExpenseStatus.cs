using System;
using System.Collections.Generic;

namespace CMS.Microservices.CMS.Models;

public partial class ExpenseStatus
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
