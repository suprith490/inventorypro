namespace InventoryPro.Api.Common;

/// <summary>
/// Base query string parameters shared by every paginated list endpoint.
/// Clamps values so a caller cannot request an enormous page and exhaust memory.
/// In Spring Boot this is the Pageable request parameter.
/// </summary>
public class PaginationQueryParameters
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 10;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    /// <summary>Number of rows to skip. EF Core equivalent of Pageable.getOffset().</summary>
    public int Skip => (Page - 1) * PageSize;
}
