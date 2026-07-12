namespace MuuqWear.API.Shared;
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasMore { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }

    /// <summary>Optional hero item (e.g. featured journal article).</summary>
    public T? FeaturedArticle { get; set; }
}