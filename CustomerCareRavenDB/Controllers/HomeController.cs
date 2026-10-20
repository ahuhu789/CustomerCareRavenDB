using System.Diagnostics;
using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Models;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.ServerWide.Operations;

namespace CustomerCareRavenDB.Controllers;

public class HomeController : Controller
{
    private readonly IRavenDbContext _ravenContext;
    private readonly IDatabaseSeeder _seeder;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IRavenDbContext ravenContext,
        IDatabaseSeeder seeder,
        ILogger<HomeController> logger)
    {
        _ravenContext = ravenContext;
        _seeder = seeder;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        // URL RavenDB/Studio lấy từ cấu hình RavenSettings:Urls (qua DocumentStore), không hardcode.
        var ravenUrl = (_ravenContext.Store.Urls.FirstOrDefault() ?? string.Empty).TrimEnd('/');
        var vm = new DashboardViewModel
        {
            DatabaseName = _ravenContext.DatabaseName,
            RavenUrl = ravenUrl,
            StudioUrl = $"{ravenUrl}/studio/index.html#databases/documents?&database={Uri.EscapeDataString(_ravenContext.DatabaseName)}"
        };

        try
        {
            vm.IsConnected = await _ravenContext.PingAsync(cancellationToken);
            if (vm.IsConnected)
            {
                var build = await _ravenContext.Store.Maintenance.Server.SendAsync(new GetBuildNumberOperation(), cancellationToken);
                vm.ServerVersion = $"RavenDB v{build.ProductVersion} (Build {build.BuildVersion})";

                using var session = _ravenContext.OpenAsyncSession();

                // Đếm số lượng document trong mỗi Collection
                vm.CustomersCount = await session.Query<Customer>().CountAsync(cancellationToken);
                vm.StaffsCount = await session.Query<Staff>().CountAsync(cancellationToken);
                vm.ServicesCount = await session.Query<Service>().CountAsync(cancellationToken);
                vm.AppointmentsCount = await session.Query<Appointment>().CountAsync(cancellationToken);
                vm.ServiceHistoriesCount = await session.Query<ServiceHistory>().CountAsync(cancellationToken);
                vm.FeedbacksCount = await session.Query<Feedback>().CountAsync(cancellationToken);

                // Lấy các dữ liệu gần nhất để hiển thị minh họa luồng
                vm.RecentCustomers = await session.Query<Customer>().Take(5).ToListAsync(cancellationToken);
                vm.RecentAppointments = await session.Query<Appointment>().Take(5).ToListAsync(cancellationToken);
                vm.RecentHistories = await session.Query<ServiceHistory>().Take(5).ToListAsync(cancellationToken);
                vm.RecentFeedbacks = await session.Query<Feedback>().Take(5).ToListAsync(cancellationToken);
            }
            else
            {
                vm.StatusMessage = $"Không thể kết nối tới RavenDB Server. Vui lòng đảm bảo Raven.Server đang chạy trên {ravenUrl} (cấu hình RavenSettings:Urls).";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi nạp dữ liệu Dashboard: {Message}", ex.Message);
            vm.IsConnected = false;
            vm.StatusMessage = $"Lỗi: {ex.Message}";
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TriggerSeed(CancellationToken cancellationToken)
    {
        try
        {
            var seeded = await _seeder.SeedAsync(cancellationToken);
            TempData["Success"] = seeded
                ? "Đã nạp dữ liệu mẫu ban đầu thành công vào CustomerCareDB!"
                : "Dữ liệu mẫu đã tồn tại từ trước (bảo toàn dữ liệu hiện có).";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi khi nạp dữ liệu mẫu: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    public IActionResult RqlQueries()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
