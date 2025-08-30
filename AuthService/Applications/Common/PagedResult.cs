namespace Common;

/// <summary>
/// DTO para paginación de resultados
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new List<T>();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
