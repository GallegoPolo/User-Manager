using AuditService.Application.Commons;
using AuditService.Application.DTOs;
using AuditService.Domain.Enums;
using MediatR;

namespace AuditService.Application.UseCases.Queries
{
    public record GetAuditLogsQuery(string? AggregateId = null,
                                    EEventType? EventType = null,
                                    DateTime? FromDate = null,
                                    DateTime? ToDate = null,
                                    int PageNumber = 1,
                                    int PageSize = 20) : IRequest<Result<PagedResultDTO<AuditLogDTO>>>;
}
