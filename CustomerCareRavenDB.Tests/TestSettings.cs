namespace CustomerCareRavenDB.Tests;

/// <summary>
/// Cấu hình dùng chung cho các bài test tích hợp.
/// URL RavenDB đọc từ biến môi trường RAVEN_TEST_URL (dùng trong CI), mặc định http://127.0.0.1:8080.
/// </summary>
internal static class TestSettings
{
    public static string RavenUrl =>
        Environment.GetEnvironmentVariable("RAVEN_TEST_URL") is { Length: > 0 } url
            ? url
            : "http://127.0.0.1:8080";
}
