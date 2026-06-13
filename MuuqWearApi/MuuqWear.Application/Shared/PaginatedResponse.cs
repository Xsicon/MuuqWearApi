namespace MuuqWear.API.Shared;
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }  // ← add setter
    public bool HasMore { get; set; }    // ← add setter
}