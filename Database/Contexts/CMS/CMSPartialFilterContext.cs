using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.Microservices.CMS.Models;
using CMS.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.Database.Contexts.CMS
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