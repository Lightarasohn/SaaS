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
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Expense>().HasQueryFilter(e => e.IsDeleted == false);
            modelBuilder.Entity<Budget>().HasQueryFilter(b => b.IsDeleted == false);
        }
    }
}