using DocumentFormat.OpenXml.InkML;
using GostObjectsClassLibrary.GostDoc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRIF.DataBaseContexts
{
    public class GostDownloadContext : DbContext
    {
        public DbSet<GostDoc> GostDocs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GostDoc>()
                .HasMany(d => d.classesAbstract)
                .WithOne()
                .HasForeignKey("GostDocAbstractId")
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<GostDoc>()
                .HasMany(d => d.classesMain)
                .WithOne()
                .HasForeignKey("GostDocMainId")
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<GostDoc>()
                .HasMany(d => d.classesEnum)
                .WithOne()
                .HasForeignKey("GostDocEnumId")
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<GostDoc>()
                .HasMany(d => d.classesCompound)
                .WithOne()
                .HasForeignKey("GostDocCompoundId")
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<GostDoc>()
                .HasMany(d => d.attributesAbstractAndMain)
                .WithOne()
                .HasForeignKey("GostDocAttributesAbstractMainId");

            modelBuilder.Entity<GostDoc>()
                .HasMany(d => d.attributesEnum)
                .WithOne()
                .HasForeignKey("GostDocAttributesEnumId");

            modelBuilder.Entity<GostDoc>()
                .HasMany(d => d.attributesCompound)
                .WithOne()
                .HasForeignKey("GostDocAttributesCompoundId");

            modelBuilder.Entity<GostDoc>()
                .HasMany(d => d.associationsAbstractAndMain)
                .WithOne()
                .HasForeignKey("GostDocAssociationsAbstractMainId");

            modelBuilder.Entity<GostDoc>()
                .HasMany(d => d.classesCimDatatype)
                .WithOne()
                .HasForeignKey("GostDocCimDatatypeId")
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<GostDoc>()
                .HasMany(d => d.attributesCimDatatype)
                .WithOne()
                .HasForeignKey("GostDocAttributesCimDatatypeId");

            modelBuilder.Entity<GostDoc>()
                .HasMany(d => d.classesPrimitive)
                .WithOne()
                .HasForeignKey("GostDocPrimitiveId")
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<GostDoc>()
                .HasKey(d => d.gostNumber);

            base.OnModelCreating(modelBuilder);
        }

        public GostDownloadContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.LogTo(Console.WriteLine);
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.UseNpgsql("Host=ia-im-sipr-ts.cdu.so;Port=5432;Database=GOSTDocs;Username=postgres;Password=rootroot;Include Error Detail=true");
        }
    }
}
