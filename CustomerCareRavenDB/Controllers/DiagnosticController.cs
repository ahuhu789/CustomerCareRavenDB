using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Data.Validation;
using CustomerCareRavenDB.Models;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;

namespace CustomerCareRavenDB.Controllers;

/// <summary>
/// Controller kiểm thử và chẩn đoán hạ tầng RavenDB.
/// Hỗ trợ thực thi truy vấn RQL trực tiếp và kiểm tra tính toàn vẹn tài liệu.
/// </summary>
public class DiagnosticController : Controller
{
    private readonly IRavenDbContext _context;
    private readonly IDocumentValidator _validator;

    public DiagnosticController(IRavenDbContext context, IDocumentValidator validator)
    {
        _context = context;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> Ping()
    {
        var isAlive = await _context.PingAsync();
        return Json(new
        {
            status = isAlive ? "Healthy" : "Unhealthy",
            database = _context.DatabaseName,
            urls = _context.Store.Urls,
            timestampUtc = DateTime.UtcNow
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExecuteRql(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Tham số RQL query không được để trống." });
        }

        try
        {
            using var session = _context.OpenAsyncSession();
            var results = await session.Advanced.AsyncRawQuery<dynamic>(query).ToListAsync();
            return Json(new
            {
                query,
                count = results.Count,
                results
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                query,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// API mẫu kiểm tra validation quan hệ giữa Appointment, ServiceHistory và Feedback.
    /// Quốc và Đạt có thể gọi endpoint này để thử nghiệm logic nghiệp vụ.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ValidateFeedbackModel([FromBody] Feedback feedback)
    {
        using var session = _context.OpenAsyncSession();
        var validation = await _validator.ValidateFeedbackAsync(session, feedback);
        return Json(new
        {
            isValid = validation.IsValid,
            errors = validation.Errors
        });
    }
}
