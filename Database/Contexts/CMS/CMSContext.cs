using System;
using System.Collections.Generic;
using CMS.Microservices.CMS.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.Database.Contexts.CMS;

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

    public virtual DbSet<Distributor> Distributors { get; set; }

    public virtual DbSet<Expense> Expenses { get; set; }

    public virtual DbSet<ExpenseCategory> ExpenseCategories { get; set; }

    public virtual DbSet<ExpenseStatus> ExpenseStatuses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=CMSConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_budget");

            entity.ToTable("budget");

            entity.HasIndex(e => e.PublicId, "budget_public_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("create_date");
            entity.Property(e => e.CreateUser).HasColumnName("create_user");
            entity.Property(e => e.DeleteDate).HasColumnName("delete_date");
            entity.Property(e => e.DeleteUser).HasColumnName("delete_user");
            entity.Property(e => e.DistributorId).HasColumnName("distributor_id");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Month).HasColumnName("month");
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

            entity.HasOne(d => d.Distributor).WithMany(p => p.Budgets)
                .HasForeignKey(d => d.DistributorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_distributor_to_budget");
        });

        modelBuilder.Entity<Distributor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_distributor");

            entity.ToTable("distributor");

            entity.HasIndex(e => e.PublicId, "distributor_public_id_key").IsUnique();

            entity.HasIndex(e => e.CompanyId, "ix_distributor_companyid");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PublicId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("public_id");
            entity.Property(e => e.Region)
                .HasMaxLength(255)
                .HasColumnName("region");
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_expense");

            entity.ToTable("expense");

            entity.HasIndex(e => e.PublicId, "expense_public_id_key").IsUnique();

            entity.HasIndex(e => e.UserId, "ix_expense_userid");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.BudgetId).HasColumnName("budget_id");
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
