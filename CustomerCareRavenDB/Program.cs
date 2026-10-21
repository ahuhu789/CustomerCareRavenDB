using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Data.Validation;
using CustomerCareRavenDB.Indexes;
using Raven.Client.Documents.Indexes;

var builder = WebApplication.CreateBuilder(args);

// 0. Hosting cho container/PaaS (Render): lắng nghe trên cổng của biến môi trường PORT nếu có.
//    Khi chạy local (không có PORT) vẫn dùng launchSettings.json / ASPNETCORE_URLS như cũ.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    if (!int.TryParse(port, out var portNumber) || portNumber is < 1 or > 65535)
    {
        throw new InvalidOperationException($"Biến môi trường PORT không hợp lệ: '{port}'.");
    }
    builder.WebHost.UseUrls($"http://0.0.0.0:{portNumber}");
}

// 1. Đăng ký cấu hình RavenSettings từ appsettings.json
builder.Services.Configure<RavenSettings>(
    builder.Configuration.GetSection(RavenSettings.SectionName));

// 2. Đăng ký RavenDbContext theo vòng đời Singleton (DocumentStore duy nhất trong suốt vòng đời ứng dụng)
builder.Services.AddSingleton<IRavenDbContext, RavenDbContext>();

// 3. Đăng ký các dịch vụ bổ trợ
builder.Services.AddScoped<IDocumentValidator, DocumentValidator>();
builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

// 4. Đăng ký MVC Controllers & Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 5. Khởi tạo cơ sở dữ liệu và Index khi ứng dụng khởi động
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var context = scope.ServiceProvider.GetRequiredService<IRavenDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    var ravenSettings = scope.ServiceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<RavenSettings>>().Value;
    var startupStep = "kiểm tra/tạo database";

    try
    {
        if (ravenSettings.EnsureDatabaseOnStartup)
        {
            logger.LogInformation("Đang kiểm tra kết nối và cấu trúc cơ sở dữ liệu RavenDB...");
            await context.EnsureDatabaseExistsAsync();
        }
        else
        {
            logger.LogInformation("Bỏ qua bước tạo database (RavenSettings:EnsureDatabaseOnStartup=false).");
        }

        // Đăng ký Static Index tự động
        startupStep = "triển khai Static Index";
        logger.LogInformation("Đang triển khai Static Index: ServiceHistories_ByCustomerAndDate...");
        await IndexCreation.CreateIndexesAsync(
            typeof(ServiceHistories_ByCustomerAndDate).Assembly,
            context.Store);
        logger.LogInformation("Static Index đã sẵn sàng.");

        // Nạp dữ liệu mẫu khi chạy Development, có tham số --seed, hoặc cấu hình Seed:OnStartup=true
        // (biến môi trường Seed__OnStartup=true). Seeder idempotent: tự bỏ qua nếu đã có dữ liệu mẫu.
        if (args.Contains("--seed")
            || app.Environment.IsDevelopment()
            || app.Configuration.GetValue<bool>("Seed:OnStartup"))
        {
            startupStep = "nạp dữ liệu mẫu (seeding)";
            logger.LogInformation("Thực hiện kiểm tra và nạp dữ liệu mẫu (Seeding)...");
            await seeder.SeedAsync();
        }
    }
    catch (Exception ex)
    {
        // Không ngắt app hoàn toàn để trang Dashboard chẩn đoán vẫn có thể hiển thị cảnh báo cho dev,
        // nhưng ghi log mức Critical, nêu rõ bước bị lỗi, URL/database và hậu quả để dễ phát hiện.
        logger.LogCritical(ex,
            "KHỞI TẠO RAVENDB THẤT BẠI ở bước '{Step}' (Urls: {Urls}, Database: {Database}): {Message}. " +
            "Ứng dụng vẫn chạy nhưng database/index/dữ liệu mẫu có thể chưa sẵn sàng; " +
            "các trang truy vấn RavenDB sẽ lỗi cho đến khi khắc phục. " +
            "Kiểm tra RavenDB Server đang chạy và cấu hình RavenSettings (Urls, DatabaseName, CertificatePath).",
            startupStep, string.Join(", ", context.Store.Urls), context.DatabaseName, ex.Message);
    }
}

// 6. Cấu hình HTTP request pipeline
// Sau reverse proxy (Render), đặt ASPNETCORE_FORWARDEDHEADERS_ENABLED=true: ASP.NET Core tự thêm
// ForwardedHeaders middleware ở đầu pipeline, tin X-Forwarded-For/X-Forwarded-Proto nên Request.IsHttps đúng.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Trong container, TLS do proxy phía trước (Render) xử lý và proxy đã tự chuyển HTTP -> HTTPS;
// app chỉ lắng nghe HTTP nên bỏ UseHttpsRedirection (tránh cảnh báo "Failed to determine the https port").
// Bật lại bằng HttpsRedirection:Enabled=true nếu cần.
var runningInContainer = string.Equals(
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);
if (app.Configuration.GetValue("HttpsRedirection:Enabled", !runningInContainer))
{
    app.UseHttpsRedirection();
}
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

// Health check cho Render: 200 khi RavenDB phản hồi, 503 nếu không (giới hạn 5 giây).
app.MapGet("/healthz", async (IRavenDbContext ravenContext, CancellationToken requestAborted) =>
{
    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
    timeout.CancelAfter(TimeSpan.FromSeconds(5));

    bool healthy;
    try
    {
        healthy = await ravenContext.PingAsync(timeout.Token).WaitAsync(timeout.Token);
    }
    catch (OperationCanceledException)
    {
        healthy = false;
    }

    var body = new { status = healthy ? "Healthy" : "Unhealthy", database = ravenContext.DatabaseName };
    return healthy ? Results.Ok(body) : Results.Json(body, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
