using System;
using System.Collections.Generic;

namespace SaaS.Microservices.CMS.Models;

public partial class Expense
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid CompanyId { get; set; }

    public Guid UserId { get; set; }

    public int BudgetId { get; set; }

    public int ExpenseCategoryId { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public int StatusId { get; set; }

    public bool IsDeleted { get; set; }

    public Guid? CreateUser { get; set; }

    public DateTime CreateDate { get; set; }

    public Guid? UpdateUser { get; set; }

    public DateTime? UpdateDate { get; set; }

    public Guid? DeleteUser { get; set; }

    public DateTime? DeleteDate { get; set; }

    public string? RejectReason { get; set; }

    public virtual Budget Budget { get; set; } = null!;

    public virtual ExpenseCategory ExpenseCategory { get; set; } = null!;

    public virtual ExpenseStatus Status { get; set; } = null!;
}
