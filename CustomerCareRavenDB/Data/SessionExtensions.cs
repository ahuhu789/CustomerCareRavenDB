using Raven.Client;
using Raven.Client.Documents.Session;

namespace CustomerCareRavenDB.Data;

public static class SessionExtensions
{
    /// <summary>
    /// Nạp document theo Id nhưng CHỈ trả về khi document thuộc đúng collection của <typeparamref name="T"/>
    /// (so sánh metadata <c>@collection</c> với quy ước tên collection của client).
    /// <para>
    /// <c>LoadAsync&lt;T&gt;(id)</c> của RavenDB không kiểm tra collection: <c>LoadAsync&lt;Customer&gt;("services/001")</c>
    /// vẫn trả về document dịch vụ ép kiểu thành Customer, khiến trang Details/Edit/Delete có thể hiển thị,
    /// ghi đè hoặc xóa nhầm document của collection khác. Document sai collection bị evict khỏi session
    /// để <c>SaveChangesAsync</c> không bao giờ ghi lại nó.
    /// </para>
    /// </summary>
    /// <returns>Document, hoặc <c>null</c> nếu Id rỗng, không tồn tại hoặc thuộc collection khác.</returns>
    public static async Task<T?> LoadFromCollectionAsync<T>(
        this IAsyncDocumentSession session, string? id, CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var entity = await session.LoadAsync<T>(id, cancellationToken);
        if (entity == null)
            return null;

        if (IsInCollection<T>(session, entity))
            return entity;

        session.Advanced.Evict(entity);
        return null;
    }

    internal static bool IsInCollection<T>(IAsyncDocumentSession session, T entity) where T : class
    {
        var expected = session.Advanced.DocumentStore.Conventions.GetCollectionName(typeof(T));
        var metadata = session.Advanced.GetMetadataFor(entity);
        return metadata.TryGetValue(Constants.Documents.Metadata.Collection, out string? actual)
               && string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }
}
