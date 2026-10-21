using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Data.Validation;
using CustomerCareRavenDB.Models;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Raven.Client.Documents;

namespace CustomerCareRavenDB.Controllers;

/// <summary>
/// Controller kiểm thử và chẩn đoán hạ tầng RavenDB.
/// Hỗ trợ thực thi truy vấn RQL trực tiếp và kiểm tra tính toàn vẹn tài liệu.
/// </summary>
public class DiagnosticController : Controller
{
    /// <summary>
    /// Khóa cấu hình bật/tắt endpoint chạy RQL trực tiếp.
    /// Mặc định: bật ở Development, tắt ở các môi trường khác.
    /// </summary>
    public const string EnableRqlEndpointKey = "Diagnostics:EnableRqlEndpoint";

    // Chỉ cho phép truy vấn đọc: chặn mệnh đề "update { ... }" (patch by query).
    private static readonly Regex UpdateClause = new(@"\bupdate\s*\{", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IRavenDbContext _context;
    private readonly IDocumentValidator _validator;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public DiagnosticController(
        IRavenDbContext context,
        IDocumentValidator validator,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _context = context;
        _validator = validator;
        _configuration = configuration;
        _environment = environment;
    }

    private bool IsRqlEndpointEnabled =>
        _configuration.GetValue<bool?>(EnableRqlEndpointKey) ?? _environment.IsDevelopment();

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteRql([FromForm] string? query)
    {
        if (!IsRqlEndpointEnabled)
        {
            return NotFound(new { error = $"Endpoint RQL đang bị tắt. Bật bằng cấu hình '{EnableRqlEndpointKey}' = true." });
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Tham số RQL query không được để trống." });
        }

        if (UpdateClause.IsMatch(query))
        {
            return BadRequest(new { query, error = "Chỉ cho phép truy vấn đọc (không hỗ trợ mệnh đề 'update')." });
        }

        try
        {
            using var session = _context.OpenAsyncSession();
            var results = await session.Advanced.AsyncRawQuery<dynamic>(query).ToListAsync();

            // Kết quả dynamic của RavenDB là object Newtonsoft (JObject),
            // System.Text.Json không tuần tự hóa được nên phải dùng Newtonsoft.Json.
            var json = JsonConvert.SerializeObject(new
            {
                query,
                count = results.Count,
                results
            });
            return Content(json, "application/json");
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
    [ValidateAntiForgeryToken]
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
