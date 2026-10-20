
using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Data.Validation;
using CustomerCareRavenDB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace CustomerCareRavenDB.Controllers;

public class AppointmentsController : Controller
{
    private readonly IRavenDbContext _context;
    private readonly IDocumentValidator _validator;

    public AppointmentsController(
        IRavenDbContext context,
        IDocumentValidator validator)
    {
        _context = context;
        _validator = validator;
    }

    // GET: /Appointments
    public async Task<IActionResult> Index(string? keyword, string? status)
    {
        using var session = _context.OpenAsyncSession();

        var query = session.Query<Appointment>();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        var appointments = await query
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();

            var customers = await session.Query<Customer>().ToListAsync();
            var customerIds = customers
                .Where(c =>
                    (c.FullName ?? "").Contains(
                        keyword, StringComparison.OrdinalIgnoreCase) ||
                    (c.Phone ?? "").Contains(
                        keyword, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Id)
                .ToHashSet();

            appointments = appointments
                .Where(a => customerIds.Contains(a.CustomerId))
                .ToList();
        }

        ViewBag.Keyword = keyword;
        ViewBag.Status = status;

        return View(appointments);
    }

    // GET: /Appointments/Details?id=appointments/1
    public async Task<IActionResult> Details(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        using var session = _context.OpenAsyncSession();
        var appointment = await session.LoadAsync<Appointment>(id);

        if (appointment == null)
            return NotFound();

        ViewBag.Customer = await session.LoadAsync<Customer>(
            appointment.CustomerId);

        ViewBag.Service = await session.LoadAsync<Service>(
            appointment.ServiceId);

        return View(appointment);
    }

    // GET: /Appointments/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        using var session = _context.OpenAsyncSession();
        await LoadOptionsAsync(session);
        return View(new Appointment());
    }

    // POST: /Appointments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Appointment appointment)
    {
        if (!ModelState.IsValid)
        {
            using var readSession = _context.OpenAsyncSession();
            await LoadOptionsAsync(readSession);
            return View(appointment);
        }

        using var session = _context.OpenAsyncSession();

        var validation = await _validator.ValidateAppointmentAsync(
            session, appointment);

        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(string.Empty, error);

            await LoadOptionsAsync(session);
            return View(appointment);
        }

        // Id = null để RavenDB tự sinh Id có prefix collection (appointments/1-A).
        appointment.Id = null!;
        appointment.CreatedAt = DateTime.UtcNow;

        await session.StoreAsync(appointment);
        await session.SaveChangesAsync();

        TempData["Success"] = "Tạo lịch hẹn thành công.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Appointments/Edit?id=appointments/1
    [HttpGet]
    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        using var session = _context.OpenAsyncSession();
        var appointment = await session.LoadAsync<Appointment>(id);

        if (appointment == null)
            return NotFound();

        await LoadOptionsAsync(session);
        return View(appointment);
    }

    // POST: /Appointments/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id, Appointment appointment)
    {
        if (id != appointment.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            using var readSession = _context.OpenAsyncSession();
            await LoadOptionsAsync(readSession);
            return View(appointment);
        }

        using var session = _context.OpenAsyncSession();
        var existing = await session.LoadAsync<Appointment>(id);

        if (existing == null)
            return NotFound();

        existing.CustomerId = appointment.CustomerId;
        existing.ServiceId = appointment.ServiceId;
        existing.AppointmentDate = appointment.AppointmentDate;
        existing.Status = appointment.Status;
        existing.Note = appointment.Note;

        var validation = await _validator.ValidateAppointmentAsync(
            session, existing);

        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(string.Empty, error);

            await LoadOptionsAsync(session);
            return View(appointment);
        }

        await session.SaveChangesAsync();

        TempData["Success"] = "Cập nhật lịch hẹn thành công.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Appointments/Delete?id=appointments/1
    [HttpGet]
    public async Task<IActionResult> Delete(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        using var session = _context.OpenAsyncSession();
        var appointment = await session.LoadAsync<Appointment>(id);

        if (appointment == null)
            return NotFound();

        return View(appointment);
    }

    // POST: /Appointments/Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        using var session = _context.OpenAsyncSession();
        var appointment = await session.LoadAsync<Appointment>(id);

        if (appointment == null)
            return NotFound();

        // Hủy lịch thay vì xóa lịch sử đặt hẹn.
        appointment.Status = "Cancelled";

        await session.SaveChangesAsync();

        TempData["Success"] = "Đã hủy lịch hẹn.";
        return RedirectToAction(nameof(Index));
    }


    private async Task LoadOptionsAsync(
        Raven.Client.Documents.Session.IAsyncDocumentSession session)
    {
        var customers = await session.Query<Customer>()
            .OrderBy(c => c.FullName)
            .ToListAsync();

        var services = await session.Query<Service>()
            .OrderBy(s => s.Name)
            .ToListAsync();

        ViewBag.Customers = new SelectList(
            customers, "Id", "FullName");

        ViewBag.Services = new SelectList(
            services, "Id", "Name");
    }

}
