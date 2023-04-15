using DataAgregation.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation
{
    public class DataContext : DbContext
    {
        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseSqlServer(@$"Data Source=Vaidual;Initial Catalog=DataAgregation0;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true");
        //}
        public DataContext(int dbIndex) 
        {
            Database.SetConnectionString(@$"Data Source=Vaidual;Initial Catalog=DataAgregation{dbIndex};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(null);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<CurrencyPurchase> CurrencyPurchases { get; set; }
        public DbSet<IngamePurchase> IngamePurchases { get; set; }
        public DbSet<StageStart> StageStarts { get; set; }
        public DbSet<StageEnd> StageEnds { get; set; }
    }
}
