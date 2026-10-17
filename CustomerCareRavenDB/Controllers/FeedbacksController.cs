using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Data.Validation;
using CustomerCareRavenDB.Models;
using CustomerCareRavenDB.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Raven.Client.Documents;

namespace CustomerCareRavenDB.Controllers;

public class FeedbacksController : Controller
{
    private readonly IRavenDbContext _context;
    private readonly IDocumentValidator _validator;

    public FeedbacksController(IRavenDbContext context, IDocumentValidator validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<IActionResult> Index(string? customerId, int? rating)
    {
        using var session = _context.OpenAsyncSession();
        IQueryable<Feedback> query = session.Query<Feedback>();
        if (!string.IsNullOrWhiteSpace(customerId)) query = query.Where(f => f.CustomerId == customerId);
        if (rating.HasValue) query = query.Where(f => f.Rating == rating.Value);
        ViewBag.CustomerId = customerId;
        ViewBag.Rating = rating;
        await LoadOptionsAsync(session);
        return View(await query.OrderByDescending(f => f.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> Details(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        using var session = _context.OpenAsyncSession();
        var item = await session.LoadAsync<Feedback>(id);
        if (item == null) return NotFound();
        ViewBag.CustomerName = (await session.LoadAsync<Customer>(item.CustomerId))?.FullName ?? item.CustomerId;
        return View(item);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        using var session = _context.OpenAsyncSession();
        await LoadOptionsAsync(session);
        return View(new FeedbackFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FeedbackFormViewModel input)
    {
        using var session = _context.OpenAsyncSession();
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(session);
            return View(input);
        }
        var appointment = await session.LoadAsync<Appointment>(input.AppointmentId);
        if (appointment == null)
        {
            ModelState.AddModelError(nameof(input.AppointmentId), "Lịch hẹn không tồn tại.");
            await LoadOptionsAsync(session);
            return View(input);
        }
        var item = new Feedback
        {
            Id = $"feedbacks/{Guid.NewGuid():N}",
            AppointmentId = appointment.Id,
            CustomerId = appointment.CustomerId,
            Rating = input.Rating,
            Comment = input.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        var validation = await _validator.ValidateFeedbackAsync(session, item);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors) ModelState.AddModelError(string.Empty, error);
            await LoadOptionsAsync(session);
            return View(input);
        }
        await session.StoreAsync(item);
        await session.SaveChangesAsync();
        TempData["Success"] = "Đã thêm phản hồi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        using var session = _context.OpenAsyncSession();
        var item = await session.LoadAsync<Feedback>(id);
        if (item == null) return NotFound();
        ViewBag.DocumentId = item.Id;
        await LoadOptionsAsync(session);
        return View(new FeedbackFormViewModel { AppointmentId = item.AppointmentId, Rating = item.Rating, Comment = item.Comment });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, FeedbackFormViewModel input)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        ViewBag.DocumentId = id;
        using var session = _context.OpenAsyncSession();
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(session);
            return View(input);
        }
        var item = await session.LoadAsync<Feedback>(id);
        if (item == null) return NotFound();
        var appointment = await session.LoadAsync<Appointment>(input.AppointmentId);
        if (appointment == null)
        {
            ModelState.AddModelError(nameof(input.AppointmentId), "Lịch hẹn không tồn tại.");
            await LoadOptionsAsync(session);
            return View(input);
        }
        item.AppointmentId = appointment.Id;
        item.CustomerId = appointment.CustomerId;
        item.Rating = input.Rating;
        item.Comment = input.Comment.Trim();
        var validation = await _validator.ValidateFeedbackAsync(session, item);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors) ModelState.AddModelError(string.Empty, error);
            await LoadOptionsAsync(session);
            return View(input);
        }
        await session.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật phản hồi.";
        return Redirect($"{Url.Action(nameof(Details), "Feedbacks")}?id={Uri.EscapeDataString(id)}");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        using var session = _context.OpenAsyncSession();
        var item = await session.LoadAsync<Feedback>(id);
        if (item == null) return NotFound();
        session.Delete(item);
        await session.SaveChangesAsync();
        TempData["Success"] = "Đã xóa phản hồi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadOptionsAsync(Raven.Client.Documents.Session.IAsyncDocumentSession session)
    {
        var appointments = await session.Query<Appointment>()
            .OrderByDescending(a => a.AppointmentDate).ToListAsync();
        var customers = await session.Query<Customer>().OrderBy(c => c.FullName).ToListAsync();
        var customerNames = customers.ToDictionary(c => c.Id, c => c.FullName);
        ViewBag.Appointments = appointments.Select(a => new SelectListItem
        {
            Value = a.Id,
            Text = $"{a.Id} - {customerNames.GetValueOrDefault(a.CustomerId, a.CustomerId)}"
        }).ToList();
        ViewBag.Customers = customers.Select(c => new SelectListItem { Value = c.Id, Text = c.FullName }).ToList();
    }
}
