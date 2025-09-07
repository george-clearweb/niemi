using Microsoft.EntityFrameworkCore;
using Niemi.Models;

namespace Niemi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SupplierInvoice> SupplierInvoices { get; set; }
        public DbSet<SupplierInvoiceRow> SupplierInvoiceRows { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SupplierInvoice>()
                .HasIndex(si => si.GivenNumber)
                .IsUnique();

            modelBuilder.Entity<SupplierInvoiceRow>()
                .HasOne(sir => sir.SupplierInvoice)
                .WithMany(si => si.SupplierInvoiceRows)
                .HasForeignKey(sir => sir.SupplierInvoiceId);

            modelBuilder.Entity<Voucher>()
                .HasOne(v => v.SupplierInvoice)
                .WithMany(si => si.Vouchers)
                .HasForeignKey(v => v.SupplierInvoiceId);
        }
    }
} 