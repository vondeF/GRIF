using GostObjectsClassLibrary.GostDoc;
using GostObjectsClassLibrary.ProfileDoc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRIF.DataBaseContexts
{
    public class ExchangeDownloadContext : DbContext
    {
        public DbSet<ProfileDoc> GostProfiles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProfileDoc>()
                .HasMany(d => d.classes)
                .WithOne()
                .HasForeignKey("profileDocId")
                .OnDelete(DeleteBehavior.ClientSetNull);

            // Не забудь вызвать base, если наследуешься от кастомного DbContext
            base.OnModelCreating(modelBuilder);
        }

        public ExchangeDownloadContext()
        {

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.LogTo(Console.WriteLine);
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.UseNpgsql("Host=ia-im-sipr-ts.cdu.so;Port=5432;Database=GOSTExchangeProfiles;Username=postgres;Password=rootroot;Include Error Detail=true");
        }
    }
}
