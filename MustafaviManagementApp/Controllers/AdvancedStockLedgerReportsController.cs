using ClosedXML.Excel;
using MedicineStore.Data;
using MedicineStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MustafaviManagementApp.Models;
using System.Text;

namespace MedicineStore.Controllers;

[AutoValidateAntiforgeryToken]                  // ← تمام POST ایکشن محفوظ
public class AdvancedStockLedgerReportsController : Controller
{
    private readonly AppDbContext _db;
    public AdvancedStockLedgerReportsController(AppDbContext db) => _db = db;

    /* ─ helpers ─ */
    private void LoadDropDowns()
    {
        ViewBag.Medicines = new SelectList(_db.Medicines.OrderBy(m => m.MedicineName),
                                           nameof(Medicine.MedicineId), nameof(Medicine.MedicineName));
        ViewBag.ActionTypes = new SelectList(new[] {
            "ALL","IN","OUT","RESERVE","RELEASE","ADJUST_IN","ADJUST_OUT","SCRAP_OUT"
        });
    }

    /* ─ UI page ─ */
    public IActionResult Index()
    {
        LoadDropDowns();
        return View();
    }

    /* ─ DataTable (POST) ─ */
    [HttpGet]
    public async Task<IActionResult> DataTable(
        DateTime? dateFrom, DateTime? dateTo,
        int? medicineId, string actionType,
        int draw, int start, int length,
        string sortCol = "CreatedAt", string sortDir = "desc")
    {
        var q = BaseQuery(dateFrom, dateTo, medicineId, actionType);

        int recordsTotal = await q.CountAsync();

        /* sort */
        q = (sortCol, sortDir) switch
        {
            ("MedicineName","asc")  => q.OrderBy(l=>l.Medicine.MedicineName),
            ("MedicineName","desc") => q.OrderByDescending(l=>l.Medicine.MedicineName),
            ("QtyChange","asc")     => q.OrderBy(l=>l.QtyChange),
            ("QtyChange","desc")    => q.OrderByDescending(l=>l.QtyChange),
            _                       => q.OrderByDescending(l=>l.CreatedAt)
        };

        var data = await q.Skip(start).Take(length)
            .Select(l => new {
                l.StockLedgerId,
                date      = l.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                medicine  = l.Medicine.MedicineName,
                action    = l.ActionType,
                qtyChange = l.QtyChange,
                balance   = l.BalanceAfter,
                saleId    = l.SaleId,
                purchaseId= l.PurchaseId
            }).ToListAsync();

        return Json(new {
            draw,
            recordsTotal,
            recordsFiltered = recordsTotal,
            data
        });
    }

    /* ─ Summary (GET) ─ */
    [HttpGet]
    public async Task<IActionResult> Summary(
        DateTime? dateFrom, DateTime? dateTo,
        int? medicineId, string actionType)
    {
        var q = BaseQuery(dateFrom, dateTo, medicineId, actionType);

        var list = await q.GroupBy(l=>l.ActionType)
                          .Select(g=> new { action=g.Key, qty=g.Sum(x=>x.QtyChange) })
                          .ToListAsync();
        return Json(list);
    }

    /* ─ Chart (GET) ─ */
    [HttpGet]
    public async Task<IActionResult> Chart(
        DateTime? dateFrom, DateTime? dateTo,
        int? medicineId)
    {
        if (dateFrom==null) dateFrom = DateTime.Today.AddDays(-30);
        if (dateTo  ==null) dateTo   = DateTime.Today;

        var q = BaseQuery(dateFrom, dateTo, medicineId, "ALL");

        var list = await q.GroupBy(l=>l.CreatedAt.Date)
            .Select(g=> new {
                date = g.Key,
                InQty  = g.Where(x=>x.ActionType.EndsWith("IN")).Sum(x=>(int?)x.QtyChange)??0,
                OutQty = g.Where(x=>x.ActionType.EndsWith("OUT")||x.ActionType=="SCRAP_OUT")
                          .Sum(x=>(int?)x.QtyChange)??0
            }).OrderBy(x=>x.date).ToListAsync();

        return Json(new {
            labels = list.Select(x=>x.date.ToString("yyyy-MM-dd")),
            inData = list.Select(x=>x.InQty),
            outData= list.Select(x=>-x.OutQty)
        });
    }

    /* ─ Excel (GET) ─ */
    [HttpGet]
    public async Task<IActionResult> ExportExcel(
        DateTime? dateFrom, DateTime? dateTo,
        int? medicineId, string actionType)
    {
        var q = BaseQuery(dateFrom, dateTo, medicineId, actionType)
                .OrderBy(l=>l.CreatedAt);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Ledger");
        ws.Cell(1,1).InsertData(new[] { new {
            Date="Date", Medicine="Medicine", Action="Action",
            Qty="QtyΔ", Balance="Balance", SaleId="SaleId", PurchaseId="PurchaseId" }});

        ws.Cell(2,1).InsertData(await q.Select(l=> new {
            Date      = l.CreatedAt,
            Medicine  = l.Medicine.MedicineName,
            Action    = l.ActionType,
            Qty       = l.QtyChange,
            Balance   = l.BalanceAfter,
            SaleId    = l.SaleId,
            PurchaseId= l.PurchaseId
        }).ToListAsync());

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms); ms.Position = 0;
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Ledger_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    /* ─ CSV streamed (GET) ─ */
    [HttpGet]
    public IActionResult ExportCsv(
        DateTime? dateFrom, DateTime? dateTo,
        int? medicineId, string actionType)
    {
        var q = BaseQuery(dateFrom, dateTo, medicineId, actionType)
                .OrderBy(l=>l.CreatedAt).AsAsyncEnumerable();

        return new FileCallbackResult("text/csv", async respStream =>
        {
            await respStream.WriteAsync(Encoding.UTF8.GetBytes(
                "Date,Medicine,Action,QtyChange,Balance,SaleId,PurchaseId\n"));
            await foreach(var l in q)
            {
                var line =
                    $"{l.CreatedAt:yyyy-MM-dd HH:mm}," +
                    $"\"{l.Medicine.MedicineName}\"," +
                    $"{l.ActionType}," +
                    $"{l.QtyChange}," +
                    $"{l.BalanceAfter}," +
                    $"{l.SaleId}," +
                    $"{l.PurchaseId}\n";
                await respStream.WriteAsync(Encoding.UTF8.GetBytes(line));
            }
        })
        { FileDownloadName = $"Ledger_{DateTime.Now:yyyyMMddHHmmss}.csv" };
    }

    /* ─ base query ─ */
    private IQueryable<StockLedger> BaseQuery(
        DateTime? f, DateTime? t, int? medId, string action)
    {
        var q = _db.StockLedgers.Include(l=>l.Medicine).AsQueryable();
        if (f.HasValue)      q = q.Where(l=>l.CreatedAt.Date>=f.Value.Date);
        if (t.HasValue)      q = q.Where(l=>l.CreatedAt.Date<=t.Value.Date);
        if (medId.HasValue)  q = q.Where(l=>l.MedicineId==medId);
        if (!string.IsNullOrWhiteSpace(action) && action!="ALL")
            q = q.Where(l=>l.ActionType==action);
        return q;
    }
}

/* streamed file helper */
public class FileCallbackResult : FileResult
{
    private readonly Func<Stream,Task> _callback;
    public FileCallbackResult(string contentType, Func<Stream,Task> cb)
        : base(contentType) => _callback = cb;

    public override async Task ExecuteResultAsync(ActionContext ctx)
    {
        if (!string.IsNullOrEmpty(FileDownloadName))
            ctx.HttpContext.Response.Headers.ContentDisposition =
                $"attachment; filename=\"{FileDownloadName}\"";
        await _callback(ctx.HttpContext.Response.Body);
    }
}
