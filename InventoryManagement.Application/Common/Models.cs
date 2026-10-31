namespace InventoryManagement.Application.Common;

public record PagedQuery
{
    public const int MaxPageSize = 100;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public bool Descending { get; init; }

    public int EffectivePage => Math.Max(Page, 1);
    public int EffectivePageSize => Math.Clamp(PageSize, 1, MaxPageSize);
    public int Skip => (EffectivePage - 1) * EffectivePageSize;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector)
        => new(Items.Select(selector).ToList(), Page, PageSize, TotalCount);
}
