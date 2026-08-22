using System;
using System.Collections.Generic;

namespace CMS.Microservices.CMS.Models;

public partial class Expense
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public int UserId { get; set; }

    public int BudgetId { get; set; }

    public int ExpenseCategoryId { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public int StatusId { get; set; }

    public bool IsDeleted { get; set; }

    public int? CreateUser { get; set; }

    public DateTime CreateDate { get; set; }

    public int? UpdateUser { get; set; }

    public DateTime? UpdateDate { get; set; }

    public int? DeleteUser { get; set; }

    public DateTime? DeleteDate { get; set; }

    public virtual Budget Budget { get; set; } = null!;

    public virtual ExpenseCategory ExpenseCategory { get; set; } = null!;

    public virtual ExpenseStatus Status { get; set; } = null!;
}
