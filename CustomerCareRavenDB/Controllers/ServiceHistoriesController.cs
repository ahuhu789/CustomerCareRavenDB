using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Data.Validation;
using CustomerCareRavenDB.Models;
using CustomerCareRavenDB.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Raven.Client.Documents;

namespace CustomerCareRavenDB.Controllers;

public class ServiceHistoriesController : Controller
{
    private readonly IRavenDbContext _context;
    private readonly IDocumentValidator _validator;

    public ServiceHistoriesController(IRavenDbContext context, IDocumentValidator validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<IActionResult> Index(string? customerId, string? staffId,
        string? serviceId, DateTime? from, DateTime? to)
    {
        using var session = _context.OpenAsyncSession();
        IQueryable<ServiceHistory> query = session.Query<ServiceHistory>();
        if (!string.IsNullOrWhiteSpace(customerId)) query = query.Where(h => h.CustomerId == customerId);
        if (!string.IsNullOrWhiteSpace(staffId)) query = query.Where(h => h.StaffId == staffId);
        if (!string.IsNullOrWhiteSpace(serviceId)) query = query.Where(h => h.ServiceId == serviceId);
        if (from.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc);
            query = query.Where(h => h.ServiceDate >= startUtc);
        }
        if (to.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(h => h.ServiceDate < endUtc);
        }
        ViewBag.CustomerId = customerId;
        ViewBag.StaffId = staffId;
        ViewBag.ServiceId = serviceId;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        await LoadOptionsAsync(session);
        return View(await query.OrderByDescending(h => h.ServiceDate).ToListAsync());
    }

    public async Task<IActionResult> Details(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        using var session = _context.OpenAsyncSession();
        var item = await session.LoadFromCollectionAsync<ServiceHistory>(id);
        if (item == null) return NotFound();
        ViewBag.CustomerName = (await session.LoadFromCollectionAsync<Customer>(item.CustomerId))?.FullName ?? item.CustomerId;
        ViewBag.StaffName = (await session.LoadFromCollectionAsync<Staff>(item.StaffId))?.FullName ?? item.StaffId;
        ViewBag.ServiceName = (await session.LoadFromCollectionAsync<Service>(item.ServiceId))?.Name ?? item.ServiceId;
        return View(item);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        using var session = _context.OpenAsyncSession();
        await LoadOptionsAsync(session);
        return View(new ServiceHistoryFormViewModel { ServiceDate = DateTime.UtcNow });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceHistoryFormViewModel input)
    {
        using var session = _context.OpenAsyncSession();
        if (input.ServiceDate == default)
            ModelState.AddModelError(nameof(input.ServiceDate), "Ngày thực hiện không hợp lệ.");
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(session);
            return View(input);
        }
        var appointment = await session.LoadFromCollectionAsync<Appointment>(input.AppointmentId);
        if (appointment == null)
        {
            ModelState.AddModelError(nameof(input.AppointmentId), "Lịch hẹn không tồn tại.");
            await LoadOptionsAsync(session);
            return View(input);
        }
        var item = new ServiceHistory
        {
            Id = $"servicehistories/{Guid.NewGuid():N}",
            AppointmentId = appointment.Id,
            CustomerId = appointment.CustomerId,
            ServiceId = appointment.ServiceId,
            StaffId = input.StaffId,
            ServiceDate = DateTime.SpecifyKind(input.ServiceDate, DateTimeKind.Utc),
            Description = input.Description.Trim(),
            Result = input.Result?.Trim() ?? "",
            Note = input.Note?.Trim() ?? "",
            CreatedAt = DateTime.UtcNow
        };
        var validation = await _validator.ValidateServiceHistoryAsync(session, item);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors) ModelState.AddModelError(string.Empty, error);
            await LoadOptionsAsync(session);
            return View(input);
        }
        await session.StoreAsync(item);
        await session.SaveChangesAsync();
        TempData["Success"] = "Đã lưu lịch sử chăm sóc.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        using var session = _context.OpenAsyncSession();
        var item = await session.LoadFromCollectionAsync<ServiceHistory>(id);
        if (item == null) return NotFound();
        ViewBag.DocumentId = item.Id;
        await LoadOptionsAsync(session);
        return View(new ServiceHistoryFormViewModel
        {
            AppointmentId = item.AppointmentId,
            StaffId = item.StaffId,
            ServiceDate = item.ServiceDate,
            Description = item.Description,
            Result = item.Result,
            Note = item.Note
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ServiceHistoryFormViewModel input)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        ViewBag.DocumentId = id;
        using var session = _context.OpenAsyncSession();
        if (input.ServiceDate == default)
            ModelState.AddModelError(nameof(input.ServiceDate), "Ngày thực hiện không hợp lệ.");
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(session);
            return View(input);
        }
        var item = await session.LoadFromCollectionAsync<ServiceHistory>(id);
        if (item == null) return NotFound();
        var appointment = await session.LoadFromCollectionAsync<Appointment>(input.AppointmentId);
        if (appointment == null)
        {
            ModelState.AddModelError(nameof(input.AppointmentId), "Lịch hẹn không tồn tại.");
            await LoadOptionsAsync(session);
            return View(input);
        }
        item.AppointmentId = appointment.Id;
        item.CustomerId = appointment.CustomerId;
        item.ServiceId = appointment.ServiceId;
        item.StaffId = input.StaffId;
        item.ServiceDate = DateTime.SpecifyKind(input.ServiceDate, DateTimeKind.Utc);
        item.Description = input.Description.Trim();
        item.Result = input.Result?.Trim() ?? "";
        item.Note = input.Note?.Trim() ?? "";
        var validation = await _validator.ValidateServiceHistoryAsync(session, item);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors) ModelState.AddModelError(string.Empty, error);
            await LoadOptionsAsync(session);
            return View(input);
        }
        await session.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật lịch sử chăm sóc.";
        return Redirect($"{Url.Action(nameof(Details), "ServiceHistories")}?id={Uri.EscapeDataString(id)}");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        using var session = _context.OpenAsyncSession();
        var item = await session.LoadFromCollectionAsync<ServiceHistory>(id);
        if (item == null) return NotFound();
        var feedbacks = await session.Query<Feedback>()
            .Where(f => f.AppointmentId == item.AppointmentId).Take(1).ToListAsync();
        if (feedbacks.Count > 0)
        {
            TempData["Error"] = "Lịch hẹn đã có phản hồi, không xóa lịch sử này.";
            return RedirectToAction(nameof(Index));
        }
        session.Delete(item);
        await session.SaveChangesAsync();
        TempData["Success"] = "Đã xóa lịch sử chăm sóc.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadOptionsAsync(Raven.Client.Documents.Session.IAsyncDocumentSession session)
    {
        var appointments = await session.Query<Appointment>()
            .OrderByDescending(a => a.AppointmentDate).ToListAsync();
        var customers = await session.Query<Customer>().ToListAsync();
        var services = await session.Query<Service>().ToListAsync();
        var staffs = await session.Query<Staff>().OrderBy(s => s.FullName).ToListAsync();
        var customerNames = customers.ToDictionary(c => c.Id, c => c.FullName);
        var serviceNames = services.ToDictionary(s => s.Id, s => s.Name);
        ViewBag.Appointments = appointments.Select(a => new SelectListItem
        {
            Value = a.Id,
            Text = $"{a.Id} - {customerNames.GetValueOrDefault(a.CustomerId, a.CustomerId)} / {serviceNames.GetValueOrDefault(a.ServiceId, a.ServiceId)}"
        }).ToList();
        ViewBag.Staffs = staffs.Select(s => new SelectListItem { Value = s.Id, Text = s.FullName }).ToList();
        ViewBag.Customers = customers.Select(c => new SelectListItem { Value = c.Id, Text = c.FullName }).ToList();
        ViewBag.Services = services.Select(s => new SelectListItem { Value = s.Id, Text = s.Name }).ToList();
    }
}
