using MedicineStore.Models;
using Microsoft.EntityFrameworkCore;
using MustafaviManagementApp.Models;
namespace MedicineStore.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<MedicineStore.Models.Store> Stores { get; set; }
    public DbSet<MedicineStore.Models.Staff> Staffs { get; set; }
    public DbSet<MedicineStore.Models.Supplier> Suppliers { get; set; }
    public DbSet<MedicineStore.Models.Category> Categorys { get; set; }
    public DbSet<MedicineStore.Models.Medicine> Medicines { get; set; }
    public DbSet<MedicineStore.Models.Inventory> Inventorys { get; set; }
    public DbSet<MedicineStore.Models.Customer> Customers { get; set; }
    public DbSet<MedicineStore.Models.Purchase> Purchases { get; set; }
    public DbSet<MedicineStore.Models.PurchaseDetail> PurchaseDetails { get; set; }
    public DbSet<MedicineStore.Models.Sale> Sales { get; set; }
    public DbSet<MedicineStore.Models.SaleDetail> SaleDetails { get; set; }
    public DbSet<MedicineStore.Models.Prescription> Prescriptions { get; set; }
    public DbSet<MedicineStore.Models.Payment> Payments { get; set; }
    public DbSet<MedicineStore.Models.PaymentDetail> PaymentDetails { get; set; }
    public DbSet<MedicineStore.Models.DailySummary> DailySummarys { get; set; }
    public DbSet<StockLedger> StockLedgers { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            /*  BREAK the extra cascade path  */
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Store)
                .WithMany(st => st.Staffs)
                .HasForeignKey(s => s.StoreId)
                .OnDelete(DeleteBehavior.Restrict);   // or .NoAction() in EF-Core 5+


            modelBuilder.Entity<PurchaseDetail>()
        .HasOne(pd => pd.Purchase)
        .WithMany(p => p.PurchaseDetails)
        .HasForeignKey(pd => pd.PurchaseId)
        // turn off cascade here:
        .OnDelete(DeleteBehavior.Restrict);
        }


    }



}
