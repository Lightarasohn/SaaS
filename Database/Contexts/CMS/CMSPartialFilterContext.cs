using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.Microservices.CMS.Models;
using SaaS.Models;
using Microsoft.EntityFrameworkCore;

namespace SaaS.Database.Contexts.CMS
{
    public partial class CMSContext : DbContext
    {
        public Guid? CurrentCompanyId { get; set; }
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrgUnit>()
            .HasQueryFilter(d => d.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<ExpenseCategory>()
                .HasQueryFilter(ec => ec.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<Budget>()
                .HasQueryFilter(b => b.CompanyId == CurrentCompanyId && b.IsDeleted == false);

            modelBuilder.Entity<Expense>()
                .HasQueryFilter(e => e.CompanyId == CurrentCompanyId && e.IsDeleted == false);
        }
    }
}