using MedicineStore.Data;
using Microsoft.EntityFrameworkCore;

namespace MustafaviManagementApp.Services
{
    public class HoldOrderCleaner: BackgroundService
    {
        private readonly IServiceProvider _sp;
        public HoldOrderCleaner(IServiceProvider sp) => _sp = sp;

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var expiry = DateTime.Now.AddDays(-1);
                var stale = await db.Sales.Where(s => s.PaymentStatus == "Pending" && s.CreatedAt < expiry).ToListAsync(token);
                if (stale.Any())
                {
                    db.Sales.RemoveRange(stale);
                    await db.SaveChangesAsync(token);
                }
                await Task.Delay(TimeSpan.FromHours(1), token);
            }
        }
    }
}
