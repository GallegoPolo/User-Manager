using AuditService.Application.Commons;
using AuditService.Application.DTOs;
using MediatR;

namespace AuditService.Application.UseCases.Queries
{
    public record GetAuditLogByIdQuery(string Id) : IRequest<Result<AuditLogDTO>>;
}
