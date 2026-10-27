using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace CustomerCareRavenDB.Tests;

internal static class TestHelpers
{
    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    /// <summary>TempData giả cho controller trong test (controller ghi thông báo TempData["Success"]).</summary>
    public static ITempDataDictionary TempData() => new TempDataDictionary(new DefaultHttpContext(), new NullTempDataProvider());
}
