using CustomerCareRavenDB.Data;
using Microsoft.Extensions.Options;

namespace CustomerCareRavenDB.Tests;

public class RavenConnectionTests
{
    [Fact]
    public void RavenDbContext_Throws_WhenUrlsAreEmpty()
    {
        var settings = Options.Create(new RavenSettings
        {
            Urls = [],
            DatabaseName = "CustomerCareDB"
        });

        Assert.Throws<ArgumentException>(() => new RavenDbContext(settings));
    }

    [Fact]
    public void RavenDbContext_Throws_WhenDatabaseNameIsEmpty()
    {
        var settings = Options.Create(new RavenSettings
        {
            Urls = ["http://127.0.0.1:8080"],
            DatabaseName = ""
        });

        Assert.Throws<ArgumentException>(() => new RavenDbContext(settings));
    }

    [Fact]
    public async Task RavenDbContext_PingAsync_ReturnsTrue_WhenServerIsOnline()
    {
        var settings = Options.Create(new RavenSettings
        {
            Urls = ["http://127.0.0.1:8080"],
            DatabaseName = "CustomerCareDB"
        });

        using var context = new RavenDbContext(settings);
        var isAlive = await context.PingAsync();

        Assert.True(isAlive, "RavenDB server tại http://127.0.0.1:8080 phải phản hồi Ping thành công.");
    }
}
