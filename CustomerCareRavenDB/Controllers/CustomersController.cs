
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

        // Dùng DocumentQuery để nhóm các điều kiện Search trong ngoặc:
        // Status = x AND (search(FullName) OR search(Phone) OR search(Email)).
        // RavenDB không hỗ trợ string.Contains trong LINQ nên dùng full-text Search() với wildcard.
        var query = session.Advanced.AsyncDocumentQuery<Customer>();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.WhereEquals(c => c.Status, status);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();
            var term = $"*{keyword}*";

            if (!string.IsNullOrWhiteSpace(status))
                query = query.AndAlso();

            query = query
                .OpenSubclause()
                    .Search(c => c.FullName, term)
                    .OrElse().Search(c => c.Phone, term)
                    .OrElse().Search(c => c.Email, term)
                .CloseSubclause();
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

        // Id = null để RavenDB tự sinh Id có prefix collection (customers/1-A).
        customer.Id = null!;
        customer.Gender = CustomerGender.Normalize(customer.Gender);
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

        // Dữ liệu cũ có thể lưu "Nam"/"Nữ": chuẩn hóa để form chọn đúng option (không lưu lại ở đây).
        customer.Gender = CustomerGender.Normalize(customer.Gender);

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
        existing.Gender = CustomerGender.Normalize(customer.Gender);
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
