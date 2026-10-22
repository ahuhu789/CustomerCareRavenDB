using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Data.Validation;
using CustomerCareRavenDB.Indexes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Raven.Client.Documents.Indexes;
using Raven.Client.ServerWide.Operations;

namespace CustomerCareRavenDB.Tests;

/// <summary>
/// Database RavenDB tạm thời cho integration test: tạo mới, deploy static index, nạp dữ liệu mẫu,
/// xóa khi kết thúc.
/// </summary>
public abstract class TestDatabase : IAsyncLifetime
{
    protected string DbName { get; } = $"Test_{Guid.NewGuid():N}";
    protected RavenDbContext Context { get; private set; } = null!;
    protected DocumentValidator Validator { get; } = new();

    /// <summary>Có nạp dữ liệu mẫu (DatabaseSeeder) trước mỗi test hay không.</summary>
    protected virtual bool Seed => true;

    public async Task InitializeAsync()
    {
        Context = new RavenDbContext(
            Options.Create(new RavenSettings { Urls = [TestSettings.RavenUrl], DatabaseName = DbName }),
            NullLogger<RavenDbContext>.Instance);
        await Context.EnsureDatabaseExistsAsync(DbName);
        await IndexCreation.CreateIndexesAsync(typeof(ServiceHistories_ByCustomerAndDate).Assembly, Context.Store);
        if (Seed)
            await new DatabaseSeeder(Context, Validator, NullLogger<DatabaseSeeder>.Instance).SeedAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await Context.Store.Maintenance.Server.SendAsync(new DeleteDatabasesOperation(DbName, hardDelete: true));
        }
        catch
        {
            // Bỏ qua lỗi dọn dẹp
        }
        finally
        {
            Context.Dispose();
        }
    }
}
