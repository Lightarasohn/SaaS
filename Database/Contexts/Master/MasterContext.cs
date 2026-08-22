using System;
using System.Collections.Generic;
using CMS.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.Database.Contexts.Master;

public partial class MasterContext : DbContext
{
    public MasterContext()
    {
    }

    public MasterContext(DbContextOptions<MasterContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AppRole> AppRoles { get; set; }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<CompanySubscription> CompanySubscriptions { get; set; }

    public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public virtual DbSet<UserToken> UserTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=MasterConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_app_role");

            entity.ToTable("app_role");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_app_user");

            entity.ToTable("app_user");

            entity.HasIndex(e => e.Email, "app_user_email_key").IsUnique();

            entity.HasIndex(e => e.PublicId, "app_user_public_id_key").IsUnique();

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
            entity.Property(e => e.DistributorId).HasColumnName("distributor_id");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.IsVerified).HasColumnName("is_verified");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.PublicId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("public_id");
            entity.Property(e => e.RecoveryKeyHash).HasColumnName("recovery_key_hash");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
            entity.Property(e => e.UpdateUser).HasColumnName("update_user");

            entity.HasOne(d => d.Company).WithMany(p => p.AppUsers)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_company_to_app_user");

            entity.HasOne(d => d.Role).WithMany(p => p.AppUsers)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_role_to_app_user");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_company");

            entity.ToTable("company");

            entity.HasIndex(e => e.PublicId, "company_public_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("create_date");
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

        modelBuilder.Entity<CompanySubscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_company_subscription");

            entity.ToTable("company_subscription");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");

            entity.HasOne(d => d.Company).WithMany(p => p.CompanySubscriptions)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_company_to_subscription");

            entity.HasOne(d => d.Plan).WithMany(p => p.CompanySubscriptions)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_plan_to_subscription");
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_subscription_plan");

            entity.ToTable("subscription_plan");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.HasBudgetAccess).HasColumnName("has_budget_access");
            entity.Property(e => e.HasHrAccess).HasColumnName("has_hr_access");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_user_token");

            entity.ToTable("user_token");

            entity.HasIndex(e => e.ExpiresAt, "ix_usertoken_expiresat");

            entity.HasIndex(e => e.TokenHash, "ix_usertoken_tokenhash");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("create_date");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(512)
                .HasColumnName("token_hash");
            entity.Property(e => e.TokenType)
                .HasMaxLength(50)
                .HasColumnName("token_type");
            entity.Property(e => e.Used).HasColumnName("used");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_to_token");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
