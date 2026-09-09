using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SaaS.Microservices.CMS.Models;

namespace SaaS.Database.Contexts.CMS;

public partial class CMSContext : DbContext
{
    public CMSContext()
    {
    }

    public CMSContext(DbContextOptions<CMSContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Budget> Budgets { get; set; }

    public virtual DbSet<Expense> Expenses { get; set; }

    public virtual DbSet<ExpenseCategory> ExpenseCategories { get; set; }

    public virtual DbSet<ExpenseStatus> ExpenseStatuses { get; set; }

    public virtual DbSet<OrgUnit> OrgUnits { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=CMSConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_budget");

            entity.ToTable("budget");

            entity.HasIndex(e => e.PublicId, "budget_public_id_key").IsUnique();

            entity.HasIndex(e => e.CompanyId, "ix_budget_companyid");

            entity.HasIndex(e => e.OrgUnitId, "ix_budget_orgunitid");

            entity.HasIndex(e => new { e.OrgUnitId, e.Year, e.Month }, "ux_budget_orgunit_period")
                .IsUnique()
                .HasFilter("(is_deleted = false)");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("create_date");
            entity.Property(e => e.CreateUser).HasColumnName("create_user");
            entity.Property(e => e.DeleteDate).HasColumnName("delete_date");
            entity.Property(e => e.DeleteUser).HasColumnName("delete_user");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Month).HasColumnName("month");
            entity.Property(e => e.OrgUnitId).HasColumnName("org_unit_id");
            entity.Property(e => e.PublicId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("public_id");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2)
                .HasColumnName("total_amount");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
            entity.Property(e => e.UpdateUser).HasColumnName("update_user");
            entity.Property(e => e.UsedAmount)
                .HasPrecision(18, 2)
                .HasColumnName("used_amount");
            entity.Property(e => e.Year).HasColumnName("year");

            entity.HasOne(d => d.OrgUnit).WithMany(p => p.Budgets)
                .HasForeignKey(d => d.OrgUnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_org_unit_to_budget");
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_expense");

            entity.ToTable("expense");

            entity.HasIndex(e => e.PublicId, "expense_public_id_key").IsUnique();

            entity.HasIndex(e => e.BudgetId, "ix_expense_budgetid");

            entity.HasIndex(e => e.CompanyId, "ix_expense_companyid");

            entity.HasIndex(e => new { e.CompanyId, e.StatusId }, "ix_expense_companyid_statusid").HasFilter("(is_deleted = false)");

            entity.HasIndex(e => e.UserId, "ix_expense_userid");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.BudgetId).HasColumnName("budget_id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("create_date");
            entity.Property(e => e.CreateUser).HasColumnName("create_user");
            entity.Property(e => e.DeleteDate).HasColumnName("delete_date");
            entity.Property(e => e.DeleteUser).HasColumnName("delete_user");
            entity.Property(e => e.Description)
                .HasMaxLength(512)
                .HasColumnName("description");
            entity.Property(e => e.ExpenseCategoryId).HasColumnName("expense_category_id");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.PublicId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("public_id");
            entity.Property(e => e.RejectReason)
                .HasMaxLength(512)
                .HasColumnName("reject_reason");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
            entity.Property(e => e.UpdateUser).HasColumnName("update_user");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Budget).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.BudgetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_budget_to_expense");

            entity.HasOne(d => d.ExpenseCategory).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.ExpenseCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_category_to_expense");

            entity.HasOne(d => d.Status).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_status_to_expense");
        });

        modelBuilder.Entity<ExpenseCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_expense_category");

            entity.ToTable("expense_category");

            entity.HasIndex(e => e.PublicId, "expense_category_public_id_key").IsUnique();

            entity.HasIndex(e => e.CompanyId, "ix_expensecategory_companyid");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.PublicId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("public_id");
        });

        modelBuilder.Entity<ExpenseStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_expense_status");

            entity.ToTable("expense_status");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<OrgUnit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_org_unit");

            entity.ToTable("org_unit");

            entity.HasIndex(e => e.CompanyId, "ix_orgunit_companyid");

            entity.HasIndex(e => e.ParentId, "ix_orgunit_parentid");

            entity.HasIndex(e => e.Path, "ix_orgunit_path").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.PublicId, "org_unit_public_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.Path)
                .HasMaxLength(255)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("path");
            entity.Property(e => e.PublicId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("public_id");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("fk_parent_to_org_unit");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
