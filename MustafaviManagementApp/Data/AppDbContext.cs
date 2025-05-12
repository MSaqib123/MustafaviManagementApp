using MedicineStore.Models;
using Microsoft.EntityFrameworkCore;
using MustafaviManagementApp.Models;
namespace MedicineStore.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Store> Stores { get; set; }
    public DbSet<Staff> Staffs { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Medicine> Medicines { get; set; }
    public DbSet<Inventory> Inventorys { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<PurchaseDetail> PurchaseDetails { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleDetail> SaleDetails { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentDetail> PaymentDetails { get; set; }
    public DbSet<DailySummary> DailySummarys { get; set; }
    public DbSet<StockLedger> StockLedgers { get; set; }
    public DbSet<Category> Categorys { get; set; }
    public DbSet<ParentCategory> ParentCategories { get; set; }
    public DbSet<CategoryParentCategory> CategoryParentCategories { get; set; }




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
