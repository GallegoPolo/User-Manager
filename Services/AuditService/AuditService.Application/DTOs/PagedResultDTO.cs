namespace AuditService.Application.DTOs
{
    public record PagedResultDTO<T>(List<T> Items,
                                    int PageNumber,
                                    int PageSize,
                                    long TotalCount,
                                    int TotalPages)
    {
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
