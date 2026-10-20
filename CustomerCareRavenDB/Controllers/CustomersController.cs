
using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Models;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace CustomerCareRavenDB.Controllers;

public class CustomersController : Controller
{
    private readonly IRavenDbContext _context;

    public CustomersController(IRavenDbContext context)
    {
        _context = context;
    }

    // GET: /Customers
    public async Task<IActionResult> Index(string? keyword, string? status)
    {
        using var session = _context.OpenAsyncSession();

        var query = session.Query<Customer>();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();

            query = query.Where(c =>
                c.FullName.Contains(keyword) ||
                c.Phone.Contains(keyword) ||
                c.Email.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        var customers = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        ViewBag.Keyword = keyword;
        ViewBag.Status = status;

        return View(customers);
    }

    // GET: /Customers/Details?id=customers%2F001
    public async Task<IActionResult> Details(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        using var session = _context.OpenAsyncSession();

        var customer = await session.LoadAsync<Customer>(id);

        if (customer == null)
            return NotFound();

        return View(customer);
    }

    // GET: /Customers/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Customer());
    }

    // POST: /Customers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        if (!ModelState.IsValid)
            return View(customer);

        customer.Id = string.Empty;
        customer.CreatedAt = DateTime.UtcNow;

        using var session = _context.OpenAsyncSession();

        await session.StoreAsync(customer);
        await session.SaveChangesAsync();

        TempData["Success"] = "Thêm khách hàng thành công.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /Customers/Edit?id=customers%2F001
    [HttpGet]
    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        using var session = _context.OpenAsyncSession();

        var customer = await session.LoadAsync<Customer>(id);

        if (customer == null)
            return NotFound();

        return View(customer);
    }

    // POST: /Customers/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Customer customer)
    {
        if (id != customer.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(customer);

        using var session = _context.OpenAsyncSession();

        var existing = await session.LoadAsync<Customer>(id);

        if (existing == null)
            return NotFound();

        existing.FullName = customer.FullName;
        existing.Phone = customer.Phone;
        existing.Email = customer.Email;
        existing.Address = customer.Address ?? new CustomerAddress();
        existing.Birthday = customer.Birthday;
        existing.Gender = customer.Gender;
        existing.Status = customer.Status;

        await session.SaveChangesAsync();

        TempData["Success"] = "Cập nhật khách hàng thành công.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /Customers/Delete?id=customers%2F001
    [HttpGet]
    public async Task<IActionResult> Delete(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        using var session = _context.OpenAsyncSession();

        var customer = await session.LoadAsync<Customer>(id);

        if (customer == null)
            return NotFound();

        return View(customer);
    }

    // POST: /Customers/Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        using var session = _context.OpenAsyncSession();

        var customer = await session.LoadAsync<Customer>(id);

        if (customer == null)
            return NotFound();

        // Cần thống nhất với nhóm về các lịch hẹn
        // đang tham chiếu đến khách hàng này trước khi xóa.
        session.Delete(customer);
        await session.SaveChangesAsync();

        TempData["Success"] = "Xóa khách hàng thành công.";

        return RedirectToAction(nameof(Index));
    }
}
