using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Data.Validation;
using CustomerCareRavenDB.Indexes;
using Raven.Client.Documents.Indexes;

var builder = WebApplication.CreateBuilder(args);

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
    var startupStep = "kiểm tra/tạo database";

    try
    {
        logger.LogInformation("Đang kiểm tra kết nối và cấu trúc cơ sở dữ liệu RavenDB...");
        await context.EnsureDatabaseExistsAsync();

        // Đăng ký Static Index tự động
        startupStep = "triển khai Static Index";
        logger.LogInformation("Đang triển khai Static Index: ServiceHistories_ByCustomerAndDate...");
        await IndexCreation.CreateIndexesAsync(
            typeof(ServiceHistories_ByCustomerAndDate).Assembly,
            context.Store);
        logger.LogInformation("Static Index đã sẵn sàng.");

        // Nạp dữ liệu mẫu khi chạy Development hoặc có tham số dòng lệnh --seed (seeder tự bỏ qua nếu đã có dữ liệu)
        if (args.Contains("--seed") || app.Environment.IsDevelopment())
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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
