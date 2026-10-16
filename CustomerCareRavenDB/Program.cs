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

    try
    {
        logger.LogInformation("Đang kiểm tra kết nối và cấu trúc cơ sở dữ liệu RavenDB...");
        await context.EnsureDatabaseExistsAsync();

        // Đăng ký Static Index tự động
        logger.LogInformation("Đang triển khai Static Index: ServiceHistories_ByCustomerAndDate...");
        await IndexCreation.CreateIndexesAsync(
            typeof(ServiceHistories_ByCustomerAndDate).Assembly,
            context.Store);
        logger.LogInformation("Static Index đã sẵn sàng.");

        // Nếu có tham số dòng lệnh --seed hoặc database chưa có dữ liệu, thực hiện nạp dữ liệu mẫu
        if (args.Contains("--seed") || app.Environment.IsDevelopment())
        {
            logger.LogInformation("Thực hiện kiểm tra và nạp dữ liệu mẫu (Seeding)...");
            await seeder.SeedAsync();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Lỗi xảy ra trong quá trình khởi tạo RavenDB lúc khởi động: {Message}", ex.Message);
        // Không ngắt app hoàn toàn để trang Dashboard chẩn đoán vẫn có thể hiển thị cảnh báo cho dev
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
