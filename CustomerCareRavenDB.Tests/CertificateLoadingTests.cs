using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CustomerCareRavenDB.Data;

namespace CustomerCareRavenDB.Tests;

/// <summary>
/// Kiểm thử nạp chứng chỉ client RavenDB từ Base64 / file (.pfx) bằng chứng chỉ tự ký sinh trong test.
/// </summary>
public class CertificateLoadingTests
{
    private static byte[] CreateSelfSignedPfx(string? password)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=customercare-test-client", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            [new Oid("1.3.6.1.5.5.7.3.2")], critical: false)); // Client Authentication
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        return cert.Export(X509ContentType.Pkcs12, password);
    }

    private static void AssertUsablePrivateKey(X509Certificate2? cert)
    {
        Assert.NotNull(cert);
        Assert.True(cert!.HasPrivateKey);
        using var key = cert.GetRSAPrivateKey();
        Assert.NotNull(key);
        var data = new byte[] { 1, 2, 3 };
        var signature = key!.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var publicKey = cert.GetRSAPublicKey();
        Assert.True(publicKey!.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    [Fact]
    public void LoadClientCertificate_ReturnsNull_WhenNotConfigured()
    {
        Assert.Null(RavenDbContext.LoadClientCertificate(new RavenSettings()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("p@ss-w0rd")]
    public void LoadClientCertificate_LoadsFromBase64_WithUsablePrivateKey(string? password)
    {
        var base64 = Convert.ToBase64String(CreateSelfSignedPfx(password));

        using var cert = RavenDbContext.LoadClientCertificate(new RavenSettings
        {
            CertificateBase64 = base64,
            CertificatePassword = password
        });

        AssertUsablePrivateKey(cert);
        Assert.Equal("CN=customercare-test-client", cert!.Subject);
    }

    [Fact]
    public void LoadClientCertificate_IgnoresWhitespaceInBase64()
    {
        var base64 = Convert.ToBase64String(CreateSelfSignedPfx(null), Base64FormattingOptions.InsertLineBreaks);
        Assert.Contains('\n', base64);

        using var cert = RavenDbContext.LoadClientCertificate(new RavenSettings { CertificateBase64 = $"  {base64}\n" });

        AssertUsablePrivateKey(cert);
    }

    [Fact]
    public void LoadClientCertificate_Base64_TakesPrecedenceOverPath()
    {
        var base64 = Convert.ToBase64String(CreateSelfSignedPfx(null));

        // Path không tồn tại nhưng không được dùng vì CertificateBase64 được ưu tiên.
        using var cert = RavenDbContext.LoadClientCertificate(new RavenSettings
        {
            CertificateBase64 = base64,
            CertificatePath = "/does/not/exist.pfx"
        });

        AssertUsablePrivateKey(cert);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("p@ss-w0rd")]
    public void LoadClientCertificate_LoadsFromPath(string? password)
    {
        var path = Path.Combine(Path.GetTempPath(), $"cc-test-{Guid.NewGuid():N}.pfx");
        File.WriteAllBytes(path, CreateSelfSignedPfx(password));
        try
        {
            using var cert = RavenDbContext.LoadClientCertificate(new RavenSettings
            {
                CertificatePath = path,
                CertificatePassword = password
            });

            AssertUsablePrivateKey(cert);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadClientCertificate_Throws_WhenBase64Invalid()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            RavenDbContext.LoadClientCertificate(new RavenSettings { CertificateBase64 = "not base64 !!" }));
        Assert.Contains("CertificateBase64", ex.Message);
    }

    [Fact]
    public void LoadClientCertificate_Throws_WhenPasswordWrong()
    {
        var base64 = Convert.ToBase64String(CreateSelfSignedPfx("right"));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            RavenDbContext.LoadClientCertificate(new RavenSettings
            {
                CertificateBase64 = base64,
                CertificatePassword = "wrong"
            }));
        Assert.Contains("CertificatePassword", ex.Message);
    }

    [Fact]
    public void LoadClientCertificate_Throws_WhenPathMissing()
    {
        Assert.Throws<FileNotFoundException>(() =>
            RavenDbContext.LoadClientCertificate(new RavenSettings { CertificatePath = "/does/not/exist.pfx" }));
    }
}
