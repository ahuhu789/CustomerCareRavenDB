using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Models;
using CustomerCareRavenDB.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;

namespace CustomerCareRavenDB.Controllers;

public class ServicesController : Controller
{
    private readonly IRavenDbContext _context;
    public ServicesController(IRavenDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search)
    {
        using var session = _context.OpenAsyncSession();
        IQueryable<Service> query = session.Query<Service>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.Name.StartsWith(term) || s.Category.StartsWith(term));
        }
        ViewBag.Search = search;
        return View(await query.OrderBy(s => s.Name).ToListAsync());
    }

    public async Task<IActionResult> Details(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        using var session = _context.OpenAsyncSession();
        var item = await session.LoadAsync<Service>(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpGet]
    public IActionResult Create() => View(new ServiceFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceFormViewModel input)
    {
        if (!ModelState.IsValid) return View(input);
        using var session = _context.OpenAsyncSession();
        var item = new Service
        {
            Id = $"services/{Guid.NewGuid():N}",
            Name = input.Name.Trim(),
            Description = input.Description?.Trim() ?? "",
            Price = input.Price,
            Category = input.Category.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        await session.StoreAsync(item);
        await session.SaveChangesAsync();
        TempData["Success"] = "Đã thêm dịch vụ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        using var session = _context.OpenAsyncSession();
        var item = await session.LoadAsync<Service>(id);
        if (item == null) return NotFound();
        ViewBag.DocumentId = item.Id;
        return View(new ServiceFormViewModel
        {
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            Category = item.Category
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ServiceFormViewModel input)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        ViewBag.DocumentId = id;
        if (!ModelState.IsValid) return View(input);
        using var session = _context.OpenAsyncSession();
        var item = await session.LoadAsync<Service>(id);
        if (item == null) return NotFound();
        item.Name = input.Name.Trim();
        item.Description = input.Description?.Trim() ?? "";
        item.Price = input.Price;
        item.Category = input.Category.Trim();
        await session.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật dịch vụ.";
        return Redirect($"{Url.Action(nameof(Details), "Services")}?id={Uri.EscapeDataString(id)}");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        using var session = _context.OpenAsyncSession();
        var item = await session.LoadAsync<Service>(id);
        if (item == null) return NotFound();
        var usedInAppointments = await session.Query<Appointment>()
            .Where(a => a.ServiceId == id).Take(1).ToListAsync();
        var usedInHistories = await session.Query<ServiceHistory>()
            .Where(h => h.ServiceId == id).Take(1).ToListAsync();
        if (usedInAppointments.Count > 0 || usedInHistories.Count > 0)
        {
            TempData["Error"] = "Dịch vụ đang được sử dụng bởi lịch hẹn/lịch sử, không thể xóa.";
            return RedirectToAction(nameof(Index));
        }
        session.Delete(item);
        await session.SaveChangesAsync();
        TempData["Success"] = "Đã xóa dịch vụ.";
        return RedirectToAction(nameof(Index));
    }
}
