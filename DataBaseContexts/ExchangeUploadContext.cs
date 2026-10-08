using GostObjectsClassLibrary;
using GRIF.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRIF.DataBaseContexts
{
    public class ExchangeUploadContext: DbContext
    {
        public DbSet<ExchangeDocumentWord> ModelDocumentWord { get; set; }
        public DbSet<DocumentPart> DocumentParts { get; set; }
        public DbSet<TableBlock> TableBlocks { get; set; }
        public DbSet<TableRow> TableRows { get; set; }
        public DbSet<TableFootnote> TableFootnotes { get; set; }
        public DbSet<TextBlock> TextBlocks { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=ia-im-sipr-ts.cdu.so;Port=5432;Database=GrifDataExchange;Username=postgres;Password=rootroot;Include Error Detail=true");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<DocumentBlock>()
                .HasDiscriminator<string>("Discriminator")
                .HasValue<TableBlock>("TableBlock")
                .HasValue<TextBlock>("TextBlock");

            modelBuilder.Entity<ExchangeDocumentWord>()
                .Property(e => e.Id)
                .UseIdentityAlwaysColumn();

            modelBuilder.Entity<DocumentPart>()
                .Property(e => e.Id)
                .UseIdentityAlwaysColumn();

            modelBuilder.Entity<TableBlock>()
                .Property(e => e.Id)
                .UseIdentityAlwaysColumn();

            modelBuilder.Entity<TextBlock>()
                .Property(e => e.Id)
                .UseIdentityAlwaysColumn();

            modelBuilder.Entity<TableRow>()
                .Property(e => e.Id)
                .UseIdentityAlwaysColumn();

            modelBuilder.Entity<RowFootnote>()
                .Property(e => e.Id)
                .UseIdentityAlwaysColumn();
        }
    }
}
