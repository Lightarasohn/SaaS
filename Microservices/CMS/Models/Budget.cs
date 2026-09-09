using System;
using System.Collections.Generic;

namespace SaaS.Microservices.CMS.Models;

public partial class Budget
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid CompanyId { get; set; }

    public int DistributorId { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal UsedAmount { get; set; }

    public bool IsDeleted { get; set; }

    public Guid? CreateUser { get; set; }

    public DateTime CreateDate { get; set; }

    public Guid? UpdateUser { get; set; }

    public DateTime? UpdateDate { get; set; }

    public Guid? DeleteUser { get; set; }

    public DateTime? DeleteDate { get; set; }

    public virtual Distributor Distributor { get; set; } = null!;

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
