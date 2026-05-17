using Encyclopedia.Models;

namespace Encyclopedia.Services.Articles;

public interface IArticleFetchService
{
    Task<IReadOnlyList<Article>> FetchAllAsync(WikiSource source, CancellationToken ct = default);
    Task<Article?>               FetchOneAsync(WikiSource source, string relativePath, CancellationToken ct = default);
}
